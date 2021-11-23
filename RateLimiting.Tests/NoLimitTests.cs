using System.Threading.Tasks;
using NUnit.Framework;
using RateLimiting.LimitingStrategies;

namespace RateLimiting.Tests
{
    [TestFixture]
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