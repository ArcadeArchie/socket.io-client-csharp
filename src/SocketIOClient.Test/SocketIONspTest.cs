﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test
{
    [TestClass]
    public class SocketIONspTest : SocketIOTestBase
    {
        protected override string Uri => "http://localhost:11000/nsp";

        [TestMethod]
        public override async Task EventHiTest()
        {
            string result = null;
            var client = new SocketIO(Uri);
            client.On("hi", response =>
            {
                result = response.GetValue<string>();
            });
            client.OnConnected += async (sender, e) =>
            {
                await client.EmitAsync("hi", ".net core");
            };
            await client.ConnectAsync();
            await Task.Delay(200);
            await client.DisconnectAsync();

            Assert.AreEqual("hi .net core, You are connected to the server - nsp", result);
        }
    }
}
