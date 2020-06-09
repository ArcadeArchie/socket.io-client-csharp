﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(Environment.OSVersion);
            Console.OutputEncoding = Encoding.UTF8;
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            //var uri = new Uri("http://doghappy.wang:11000/nsp");
            var uri = new Uri("http://localhost:11000/nsp");

            var socket = new SocketIO(uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    {"token", "io" }
                },
                ConnectionTimeout = TimeSpan.FromSeconds(10)
            });

            socket.OnConnected += Socket_OnConnected;
            socket.OnPing += Socket_OnPing;
            socket.OnPong += Socket_OnPong;
            socket.OnDisconnected += Socket_OnDisconnected;
            await socket.ConnectAsync();

            socket.On("hi", response =>
            {
                Console.WriteLine($"server: {response.GetValue<string>()}");
            });

            //client.On("bytes", response =>
            //{
            //    var bytes = response.GetValue<ByteResponse>();
            //    Console.WriteLine($"bytes.Source = {bytes.Source}");
            //    Console.WriteLine($"bytes.ClientSource = {bytes.ClientSource}");
            //    Console.WriteLine($"bytes.Buffer.Length = {bytes.Buffer.Length}");
            //    Console.WriteLine($"bytes.Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
            //});
            //client.OnReceivedEvent += (sender, e) =>
            //{
            //    if (e.Event == "bytes")
            //    {
            //        var bytes = e.Response.GetValue<ByteResponse>();
            //        Console.WriteLine($"OnReceivedEvent.Source = {bytes.Source}");
            //        Console.WriteLine($"OnReceivedEvent.ClientSource = {bytes.ClientSource}");
            //        Console.WriteLine($"OnReceivedEvent.Buffer.Length = {bytes.Buffer.Length}");
            //        Console.WriteLine($"OnReceivedEvent.Buffer.ToString() = {Encoding.UTF8.GetString(bytes.Buffer)}");
            //    }
            //};


            //await socket.EmitAsync("hi", "SocketIOClient.Sample");

            //await socket.EmitAsync("ack", response =>
            //{
            //    Console.WriteLine(response.ToString());
            //}, "SocketIOClient.Sample");

            //await socket.EmitAsync("bytes", "c#", new
            //{
            //    source = "client007",
            //    bytes = Encoding.UTF8.GetBytes("dot net")
            //});

            socket.On("client binary callback", async response =>
            {
                await response.CallbackAsync();
            });

            await socket.EmitAsync("client binary callback", Encoding.UTF8.GetBytes("SocketIOClient.Sample"));

            //socket.On("client message callback", async response =>
            //{
            //    await response.CallbackAsync(Encoding.UTF8.GetBytes("CallbackAsync();"));
            //});
            //await socket.EmitAsync("client message callback", "SocketIOClient.Sample");

            Console.ReadLine();
        }

        private static async void Socket_OnDisconnected(object sender, string e)
        {
            Console.WriteLine("disconnect: " + e);
            //var client = sender as SocketIO;
            //while (true)
            //{
            //    try
            //    {
            //        await client.ConnectAsync();
            //        break;
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //        await Task.Delay(1000);
            //    }
            //}
            //for (int i = 1; i <= 10; i++)
            //{
            //    try
            //    {
            //        await client.ConnectAsync();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //        Console.WriteLine($"Wait {i} s...");
            //        await Task.Delay(TimeSpan.FromSeconds(i));
            //    }
            //}
        }

        private static async void Socket_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Socket_OnConnected");
            var client = sender as SocketIO;
            Console.WriteLine("Socket.Id:" + client.Id);
            //await client.DisconnectAsync();
        }

        private static void Socket_OnPing(object sender, EventArgs e)
        {
            Console.WriteLine("Ping");
        }

        private static void Socket_OnPong(object sender, TimeSpan e)
        {
            Console.WriteLine("Pong: " + e.TotalMilliseconds);
        }
    }

    class ByteResponse
    {
        public string ClientSource { get; set; }

        public string Source { get; set; }

        [JsonProperty("bytes")]
        public byte[] Buffer { get; set; }
    }

    class ClientCallbackResponse
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("bytes")]
        public byte[] Bytes { get; set; }
    }
}
