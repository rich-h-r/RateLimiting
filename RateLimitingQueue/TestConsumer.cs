using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RateLimitingQueue
{
    public class TestConsumer<T>
    {
        private readonly Task consumerLoop;

        public TestConsumer(ChannelReader<T> channelReader, IRateLimitingStrategy<T> rateLimitingStrategy)
        {
            consumerLoop = CreateConsumerLoop(channelReader, rateLimitingStrategy);
            Console.WriteLine("Started Consumer Loop");
        }
        
        private Task CreateConsumerLoop(ChannelReader<T> channelReader, IRateLimitingStrategy<T> rateLimitingStrategy)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (channelReader.TryRead(out var work))
                    {
                        var result = await rateLimitingStrategy.AcquireAsync(work, 1);
                        
                        if (result)
                        {
                            Console.WriteLine($"{DateTimeOffset.Now:mm:ss:ffff} CONSUMER : Processing {work}, wait {result.WaitTime:c}");
                            await Task.Delay(100); //simulate some work
                            continue;
                        }

                        Console.WriteLine($"{DateTimeOffset.Now:mm:ss:ffff} CONSUMER : Dropping {work}");
                    }
                }
            });
        }
    }
}