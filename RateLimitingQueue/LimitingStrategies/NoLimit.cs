using System.Threading;
using System.Threading.Tasks;

namespace RateLimitingQueue.LimitingStrategies
{
    /// <summary>
    /// This is just a null rate limiter that always acquires the lock
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NoLimit<T> : IRateLimitingStrategy<T>
    {
        public Task<AcquireResult> AcquireAsync(T context, int requestCount = 1, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AcquireResult.NoOp);
        }
    }
}