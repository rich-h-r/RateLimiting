using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RateLimitingQueue
{
    class MyProducer
    {
        private readonly ChannelWriter<string> channelWriter;

        public MyProducer(ChannelWriter<string> channelWriter)
        {
            this.channelWriter = channelWriter;
        }

        public Task Produce()
        {
            return Task.Factory.StartNew(async () =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        await channelWriter.WriteAsync("i");
                        if (i % 10 == 0)
                            await Task.Delay(1000);
                        else
                            await Task.Delay(100);
                    } 
                }
            );
        }
    }
}