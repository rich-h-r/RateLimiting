using System;
using System.Threading.Tasks;
using NUnit.Framework;
using RateLimiting.ConfigurationStores;
using RateLimiting.LimitingStrategies;

namespace RateLimiting.Tests
{
    [TestFixture]
    public class MovingWindowTests
    {
        private static readonly TimeSpan TenSeconds = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan FiftyMilliseconds = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan FiftyOneMilliseconds = TimeSpan.FromMilliseconds(50);
        
        [Test]
        public async Task Should_acquire()
        {
            var strategy = CreateSutWithWindowOf(TenSeconds); 
            Assert.True((await strategy.AcquireAsync("me"))?.Acquired);
        }
        
        [Test]
        public async Task Should_exhaust_requests()
        {
            var strategy = CreateSutWithWindowOf(TenSeconds); 
            await strategy.AcquireAsync("me");
            Assert.False((await strategy.AcquireAsync("me"))?.Acquired);
        }

        [Test]
        public async Task Should_support_multiple_users()
        {
            var strategy = CreateSutWithWindowOf(TenSeconds);
             
            //I should be exhausted
            await strategy.AcquireAsync("me");
            Assert.False((await strategy.AcquireAsync("me"))?.Acquired);
            
            //you should still be able to process
            Assert.True((await strategy.AcquireAsync("you"))?.Acquired);
        }

        [Test]
        public async Task Should_be_able_to_acquire_after_expiry()
        {
             var strategy = CreateSutWithWindowOf(FiftyMilliseconds);
            
             await strategy.AcquireAsync("me");
             //exhaust me
             Assert.False((await strategy.AcquireAsync("me"))?.Acquired);
             
             await Task.Delay(FiftyOneMilliseconds);
             //window should have expired
             Assert.True((await strategy.AcquireAsync("me"))?.Acquired);
        }

        [Test]
        public async Task Should_handle_zero_request_limit()
        {
            var strategy = CreateSutWithWindowOf(FiftyMilliseconds, 0);
            await strategy.AcquireAsync("me", 1);
        }

        [Test]
        public void Should_throw_ArgumentNullException()
        {
             var strategy = CreateSutWithWindowOf(FiftyMilliseconds);
             Assert.ThrowsAsync<ArgumentNullException>(async () => await strategy.AcquireAsync(null));
        }

        [Test]
        public async Task Should_return_data_about_remaining_time_window()
        {
            var strategy = CreateSutWithWindowOf(FiftyMilliseconds, 3);
            var result = await strategy.AcquireAsync("me");
            
            Assert.That(result.RemainingRequestInWindow, Is.EqualTo(2));
            Assert.That(result.RequestsMadeInWindow, Is.EqualTo(1));
            Assert.That(result.Window, Is.EqualTo(FiftyMilliseconds));
        }

        [Test]
        public async Task Should_give_an_indication_of_remaining_time()
        {
            var strategy = CreateSutWithWindowOf(FiftyMilliseconds);
            await strategy.AcquireAsync("me");
            //limit is exhausted
            var result = await strategy.AcquireAsync("me");
            
            Assert.That(result.Acquired, Is.False);
            Assert.That(result.WaitTime, Is.LessThan(FiftyMilliseconds));
            Assert.That(result.WaitTime, Is.GreaterThan(TimeSpan.Zero));
        }
        
        private IRateLimitingStrategy<string> CreateSutWithWindowOf(TimeSpan timeSpan, int requestsInWindow = 1)
        {
            return new MovingWindow<string>( new DefaultRateLimitConfigurationStore<string>(timeSpan, requestsInWindow, requestsInWindow));
        }
    }
}