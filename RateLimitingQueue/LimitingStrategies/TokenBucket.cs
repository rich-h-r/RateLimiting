using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimitingQueue.LimitingStrategies
{
    /// https://en.wikipedia.org/wiki/Token_bucket
    /// tokens added every 1/r seconds
    /// bucket can hold at most b tokens
    /// if required tokens are available, then 
    /// if no tokens available request is rejected
    public class TokenBucket<T> : IRateLimitingStrategy<T>
    {
        private readonly IRateLimitConfigurationStore<T> configurationStore;
        private readonly Dictionary<T, (int Tokens, long LastRefill, long RefillPeriod, int MaxCapacity)> tokensLeft = new Dictionary<T, (int, long, long, int)>();
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        
        public TokenBucket(IRateLimitConfigurationStore<T> configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        private async Task<(int Tokens, long LastRefill, long RefillPeriod, int MaxCapacity)> FetchStateFor(T context)
        {
            if (tokensLeft.TryGetValue(context, out var requestState))
                return requestState;
                
            var configuration = await configurationStore.GetConfigurationForAsync(context);
            var newRequestState = (configuration.RequestsLimit, DateTime.Now.Ticks, configuration.TimeWindow.Ticks, configuration.RequestsLimit);
            tokensLeft.Add(context, newRequestState);

            return newRequestState;
        }
        
        public async Task<AcquireResult> AcquireAsync(T context, int requestCount = 1, CancellationToken cancellationToken = default)
        {
            var requestState = await FetchStateFor(context);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var (tokens, lastRefill, refillPeriod, maxCapacity) = UpdateTokens(requestState);

                if (tokens < requestCount)
                    return AcquireResult.LimitRejected(TimeSpan.Zero, requestState.MaxCapacity, 0);

                tokens -= requestCount;

                tokensLeft[context] = (tokens, lastRefill, refillPeriod, maxCapacity);
                return AcquireResult.LimitAcquired(TimeSpan.Zero, requestState.MaxCapacity, 0);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private (int tokens, long LastRefill, long RefillPeriod, int MaxCapacity) UpdateTokens((int Tokens, long LastRefill, long RefillPeriod, int MaxCapacity) requestState)
        {
            var now = DateTimeOffset.Now.Ticks;
            if (now >= requestState.LastRefill)
            {
                var tokensToAdd =  (now - requestState.LastRefill) / requestState.RefillPeriod;
                if (tokensToAdd >= 1)
                {
                    var nextRefillPeriod = requestState.LastRefill + tokensToAdd * requestState.RefillPeriod;
                    return ((int)Math.Min(requestState.Tokens + tokensToAdd, requestState.MaxCapacity), now, nextRefillPeriod, requestState.MaxCapacity);
                }
            }
                
            return requestState;
        }
    }
}