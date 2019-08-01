﻿using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.Arguments;
using SocketIOClient.Parsers;

namespace SocketIOClient
{
    public class SocketIO
    {
        public SocketIO(Uri uri)
        {
            if (uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "wss" || uri.Scheme == "ws")
            {
                _uri = uri;
            }
            else
            {
                throw new ArgumentException("Unsupported protocol");
            }
            _openedParser = new OpenedParser();
            _eventHandlers = new Dictionary<string, EventHandler>();
            _urlConverter = new UrlConverter();
            if (_uri.AbsolutePath != "/")
            {
                Namespace = _uri.AbsolutePath + ',';
            }
            _parserRegex = new Regex("42" + Namespace + @"\[""(\w+)"",([\s\S]*)\]");
        }

        public SocketIO(string uri) : this(new Uri(uri)) { }

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        readonly Uri _uri;
        private ClientWebSocket _socket;
        readonly OpenedParser _openedParser;
        readonly UrlConverter _urlConverter;
        public string Namespace { get; private set; }
        readonly Regex _parserRegex;

        public int EIO { get; set; } = 3;
        public Dictionary<string, string> Parameters { get; set; }

        public event Action<OpenedArgs> OnOpened;
        //public event Action<string> OnUnknownMessageReceived;
        public event Action OnConnected;

        /// <summary>
        /// Triggered when the server disconnects rather than the client actively disconnects.
        /// </summary>
        public event Action OnClosed;

        private readonly Dictionary<string, EventHandler> _eventHandlers;

        public async Task ConnectAsync()
        {
            Uri wsUri = _urlConverter.HttpToWs(_uri, EIO.ToString(), Parameters);
            if (_socket != null)
            {
                _socket.Dispose();
            }
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(wsUri, CancellationToken.None);
            Listen();
        }

        public async Task CloseAsync()
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Call CloseAsync()", CancellationToken.None);
            _socket.Dispose();
        }

        private void Listen()
        {
            Task.Run(async () =>
            {
                var buffer = new byte[ReceiveChunkSize];
                while (true)
                {
                    var builder = new StringBuilder();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            //await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        }
                        else if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            builder.Append(str);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string text = builder.ToString();
                        Pretreatment(text);
                    }
                    //if (_socket.State != WebSocketState.Open)
                    //{

                    //}
                }
            });
        }

        private void Pretreatment(string text)
        {
            if (_openedParser.Check(text))
            {
                JObject jobj = _openedParser.Parse(text);
                var args = jobj.ToObject<OpenedArgs>();
                OnOpened?.Invoke(args);
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(args.PingInterval);
                        await SendMessageAsync(((int)EngineIOProtocol.Ping).ToString());
                    }
                });
            }
            else if (text == "40" + Namespace)
            {
                OnConnected?.Invoke();
            }
            else if (text == "41" + Namespace)
            {
                OnClosed?.Invoke();
            }
            else if (_parserRegex.IsMatch(text))
            {
                var groups = _parserRegex.Match(text).Groups;
                string eventName = groups[1].Value;
                if (_eventHandlers.ContainsKey(eventName))
                {
                    var handler = _eventHandlers[eventName];
                    handler(new ResponseArgs
                    {
                        Text = groups[2].Value,
                        RawText = text
                    });
                }
            }
            //else
            //{
            //    OnUnknownMessageReceived?.Invoke(text);
            //}
        }

        /// <summary>
        /// Send text messages directly instead of wrapped messages, usually you don't need to call this method, 
        /// messages sent by calling this method may not be processed by the SocketIO server.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task SendMessageAsync(string text)
        {
            if (_socket.State == WebSocketState.Open)
            {
                var messageBuffer = Encoding.UTF8.GetBytes(text);
                var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

                for (var i = 0; i < messagesCount; i++)
                {
                    int offset = SendChunkSize * i;
                    int count = SendChunkSize;
                    bool isEndOfMessage = (i + 1) == messagesCount;

                    if ((count * (i + 1)) > messageBuffer.Length)
                    {
                        count = messageBuffer.Length - offset;
                    }

                    await _socket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, isEndOfMessage, CancellationToken.None);
                }
            }
        }

        public void On(string eventName, EventHandler handler)
        {
            _eventHandlers.Add(eventName, handler);
        }

        public async Task EmitAsync(string eventName, object obj)
        {
            string text = JsonConvert.SerializeObject(obj);
            var builder = new StringBuilder();
            builder
                .Append("42")
                .Append(Namespace)
                .Append('[')
                .Append('"')
                .Append(eventName)
                .Append('"')
                .Append(',')
                .Append(text)
                .Append(']');

            await SendMessageAsync(builder.ToString());
        }
    }
}
