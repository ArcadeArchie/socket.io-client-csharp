﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new SocketIO("http://localhost:3000");

            client.On("test", args =>
            {
                string text = JsonConvert.DeserializeObject<string>(args.Text);
                Console.WriteLine(text);
            });

            client.OnConnected += async () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    await client.EmitAsync("test", i.ToString());
                    await Task.Delay(1000);
                }
            };

            client.OnClosed += Client_OnClosed;

            await client.ConnectAsync();
            Console.ReadLine();
        }

        private static void Client_OnClosed(ServerCloseReason reason)
        {
            Console.WriteLine("reason: " + reason.ToString());
        }
    }
}
