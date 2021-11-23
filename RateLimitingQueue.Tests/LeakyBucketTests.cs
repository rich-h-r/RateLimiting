using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using RateLimitingQueue.LimitingStrategies;

namespace RateLimitingQueue.Tests
{
    [TestFixture]
    public class LeakyBucketTests
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
         public async Task Should_have_a_maximum_bucket_size()
         {
             var sut = CreateSut();
             Assert.IsTrue(await sut.AcquireAsync("me"));
             //try to accumulate more than the 1 token
             await Task.Delay(FiftyMilliseconds);
             Assert.IsTrue(await sut.AcquireAsync("me"));
             Assert.IsFalse(await sut.AcquireAsync("me")); 
         }

         [Test]
         public async Task Should_delay_if_more_than_request_limit()
         {
             var sut = CreateSut(2);
             //need single call to load user into context 
             Assert.IsTrue(await sut.AcquireAsync("me"));
             //try to accumulate more than the 1 token
             await Task.Delay(FiftyMilliseconds);
             await Task.Delay(FiftyMilliseconds);
             //exhaust the first limit
             Assert.IsTrue(await sut.AcquireAsync("me"));
             //should now be delayed till we get more tokens
             var result = await sut.AcquireAsync("me");
             Assert.IsTrue(result);
             Assert.That(result.WaitTime.Ticks, Is.GreaterThan(0));
         }
 
         private LeakyBucket<string> CreateSut(int maxRequests=1)
         {
             var mock = new Mock<IRateLimitConfigurationStore<string>>();
             mock.Setup(x => x.GetConfigurationForAsync("me"))
                 .Returns(Task.FromResult(new RateLimitConfiguration { RequestsLimit = 1, TimeWindow = FiftyMilliseconds, MaxCapacity = maxRequests}));
             return new LeakyBucket<string>(mock.Object);
         }       
    }
}