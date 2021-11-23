using System.Threading.Tasks;
using NUnit.Framework;
using RateLimitingQueue.LimitingStrategies;

namespace RateLimitingQueue.Tests
{
    public class NoLimitTests
    {
        [Test]
        public async Task Should_always_return_true()
        {
            var sut = new NoLimit<string>();
            Assert.IsTrue(await sut.AcquireAsync(null));
        }
        
    }
}