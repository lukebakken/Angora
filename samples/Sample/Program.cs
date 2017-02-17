﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQClient;

namespace Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbit"
            };

            var connection = await factory.CreateConnection("test connection");

            var channel = await connection.CreateChannel();

            var arguments = new Dictionary<string, object>
            {
                { "x-queue-mode", "lazy" },
                { "x-message-ttl", 3000 }
            };

            var test1Result = await channel.Queue.Declare("test1", false, true, false, false, arguments);
            var test2Result = await channel.Queue.Declare("test2", false, true, false, false, null);
            var generatedResult = await channel.Queue.Declare("", false, true, true, false, null);

            await channel.Exchange.Declare("test1", "fanout", false, true, false, false, null);
            await channel.Exchange.Declare("test2", "fanout", false, true, false, false, null);
            await channel.Exchange.Declare("test3", "fanout", false, true, false, false, null);

            await channel.Exchange.Bind("test1", "test3", "key", arguments);
            await channel.Exchange.Unbind("test1", "test3", "key", arguments);

            await channel.Exchange.Declare("test-internal", "fanout", false, true, false, true, null);

            await channel.Queue.Bind("test1", "test1", "", null);

            await channel.Queue.Bind("test2", "test1", "foo", null);
            await channel.Queue.Unbind("test2", "test1", "foo", null);

            var purgeCount = await channel.Queue.Purge("test2");

            var deleteCount = await channel.Queue.Delete("test2", true, true);

            await channel.Exchange.Delete("test2", false);

            await channel.Basic.Qos(0, 100, false);

            Console.WriteLine("Press any key to quit");
            Console.ReadKey();

            await channel.Close();

            await connection.Close();
        }
    }
}
