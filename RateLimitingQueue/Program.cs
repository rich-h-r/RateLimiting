using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RateLimitingQueue.ConfigurationStores;
using RateLimitingQueue.LimitingStrategies;

namespace RateLimitingQueue
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("hello");
            var configurationStore = new DefaultRateLimitConfigurationStore<string>(TimeSpan.FromMilliseconds(10), 20, 40);
            var movingWindowStrategy = new LeakyBucket<string>(configurationStore);
            int succeededCount = 0;
            var writers = new List<Task>();
            for (int i = 0; i < 70; i++)
            {
                var index = i;
                if (index == 40)
                    await Task.Delay(200);
                var task = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var result = await movingWindowStrategy.AcquireAsync("me");
                        await Task.Delay(result.WaitTime);
                        if (result)
                            Interlocked.Increment(ref succeededCount);
                        Console.WriteLine(
                            $"{index}: Acquired resource: {result.Acquired}, delayed:{result.WaitTime >= TimeSpan.Zero} requests made: {result.RequestsMadeInWindow} remaining request {result.RemainingRequestInWindow}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
                writers.Add(task);
            }

            await Task.WhenAll(writers);
            Console.Read();

            Console.WriteLine($"total succeeded: {succeededCount}");
        }

        private async Task SimpleProducerConsumerQueue()
        {
            var channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });

            var readerTask = Task.Factory.StartNew(async () =>
            {
                var reader = channel.Reader;
                while (true)
                {
                    var input = await reader.ReadAsync();
                    Console.WriteLine($"Message working: Thread [{Thread.CurrentThread.ManagedThreadId}] running job [{input}]");
                    await Task.Delay(200);
                }
            });
            
            var writers = new List<Task>();
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                var task = Task.Factory.StartNew(async () =>
                {
                    Console.WriteLine($"Message received: Thread [{Thread.CurrentThread.ManagedThreadId}] running job [{index}]");
                    await channel.Writer.WriteAsync(index);
                });
                writers.Add(task);
            }

            await Task.WhenAll(writers);
            Console.Read();
        }


        private async void TestRateLimiter()
        {
            Console.WriteLine("Started ");
            var myChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions{SingleReader = true});
            var configurationStore = new DefaultRateLimitConfigurationStore<string>(TimeSpan.FromSeconds(1), 5, 5);
            var _ = new TestConsumer<string>(myChannel.Reader, new MovingWindow<string>(configurationStore));
            var producer = new MyProducer(myChannel.Writer);
            await producer.Produce();
            Console.ReadLine();
        }
    }
}