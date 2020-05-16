﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient.Test.Models;
using System.Collections.Generic;
using SocketIOClient.EventArguments;

namespace SocketIOClient.Test
{
    [TestClass]
    public abstract class SocketIOTestBase
    {
        protected abstract string Uri { get; }

        [TestMethod]
        public async Task OnConnectedTest()
        {
            bool result = false;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += (sender, e) => result = true;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();
            Assert.IsTrue(result);
        }

        public abstract Task EventHiTest();

        [TestMethod]
        public async Task EventAckTest()
        {
            JToken result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("ack", response =>
                {
                    result = response.GetValue();
                }, ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.IsTrue(result.Value<bool>("result"));
            Assert.AreEqual("ack(.net core)", result.Value<string>("message"));
        }

        [TestMethod]
        public async Task BinaryEventTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.On("bytes", response => result = response.GetValue<ByteResponse>());

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("bytes", name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }

        [TestMethod]
        public async Task ServerDisconectTest()
        {
            string reason = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("sever disconnect", false);
            };
            client.OnDisconnected += (sender, e) => reason = e;
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("io server disconnect", reason);
        }

        [TestMethod]
        public async Task BinaryAckTest()
        {
            ByteResponse result = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("binary ack", response =>
                {
                    result = response.GetValue<ByteResponse>();
                }, name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }

        [TestMethod]
        public async Task EventChangeTest()
        {
            string resVal1 = null;
            ChangeResponse resVal2 = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("change", new
                {
                    code = 200,
                    message = "val1"
                }, "val2");
            };
            client.On("change", response =>
            {
                resVal1 = response.GetValue<string>();
                resVal2 = response.GetValue<ChangeResponse>(1);
            });
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("val2", resVal1);
            Assert.AreEqual(200, resVal2.Code);
            Assert.AreEqual("val1", resVal2.Message);
        }

        [TestMethod]
        public async Task OnReceivedBinaryEventTest()
        {
            ReceivedEventArgs args = null;
            var client = new SocketIO(Uri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                }
            });
            client.OnReceivedEvent += (sender, e) => args = e;

            const string dotNetCore = ".net core";
            const string client001 = "client001";
            const string name = "unit test";

            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("bytes", name, new
                {
                    source = client001,
                    bytes = Encoding.UTF8.GetBytes(dotNetCore)
                });
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("bytes", args.Event);

            var result = args.Response.GetValue<ByteResponse>();
            Assert.AreEqual("client001", result.ClientSource);
            Assert.AreEqual("server", result.Source);
            Assert.AreEqual($"{dotNetCore} - server - {name}", Encoding.UTF8.GetString(result.Buffer));
        }
    }
}
