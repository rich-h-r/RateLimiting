using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using RateLimiting.LimitingStrategies;

namespace RateLimiting.Tests
{
    [TestFixture]
    public class TokenBucketTests
    {
        private static readonly TimeSpan FiftyMilliseconds = TimeSpan.FromMilliseconds(50);
        
        [Test]
        public async Task Should_process_request_if_enough_tokens()
        {
            var sut = CreateSut();
            Assert.IsTrue(await sut.AcquireAsync("me")); 
        }
        
        [Test]
        public async Task Should_reject_request_if_limit_exceeded()
        {
            var sut = CreateSut();
            Assert.IsTrue(await sut.AcquireAsync("me")); 
            Assert.IsFalse(await sut.AcquireAsync("me")); 
        }

        [Test]
        public async Task Should_add_tokens_after_required_period()
        {
            var sut = CreateSut();
            Assert.IsTrue(await sut.AcquireAsync("me"));
            await Task.Delay(FiftyMilliseconds);
            Assert.IsTrue(await sut.AcquireAsync("me"));
        }

        [Test]
        public async Task Should_have_a_maximum_bucket_size_of_initial_bucket_size()
        {
            var sut = CreateSut();
            Assert.IsTrue(await sut.AcquireAsync("me"));
            //try to accumulate more than the 1 token
            await Task.Delay(FiftyMilliseconds);
            await Task.Delay(FiftyMilliseconds);
            Assert.IsTrue(await sut.AcquireAsync("me"));
            Assert.IsFalse(await sut.AcquireAsync("me")); 
        }

        private TokenBucket<string> CreateSut()
        {
            var mock = new Mock<IRateLimitConfigurationStore<string>>();
            mock.Setup(x => x.GetConfigurationForAsync("me"))
                .Returns(Task.FromResult(new RateLimitConfiguration { RequestsLimit = 1, TimeWindow = FiftyMilliseconds, MaxCapacity = 2}));
            return new TokenBucket<string>(mock.Object);
        }
    }
}