using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiting.LimitingStrategies
{
    public class LeakyBucket<T> : IRateLimitingStrategy<T>
    {
        private readonly IRateLimitConfigurationStore<T> configurationStore;
        private readonly Dictionary<T, (int RemainingTokens, int TokenLimit, long LastRefill, long RefillPeriod, int MaxCapacity)> tokensLeft = new Dictionary<T, (int, int, long, long, int)>();
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        
        public LeakyBucket(IRateLimitConfigurationStore<T> configurationStore)
        {
            this.configurationStore = configurationStore;
        }

        private async Task<(int Tokens, int RemainingTokens, long LastRefill, long RefillPeriod, int MaxCapacity)> FetchStateFor(T context, long now)
        {
            if (tokensLeft.TryGetValue(context, out var requestState))
                return requestState;
                
            var configuration = await configurationStore.GetConfigurationForAsync(context);
            var newRequestState = (configuration.MaxCapacity, configuration.RequestsLimit, now, configuration.TimeWindow.Ticks, configuration.MaxCapacity);
            tokensLeft.Add(context, newRequestState);

            return newRequestState;
        }
        
        public async Task<AcquireResult> AcquireAsync(T context, int requestCount = 1, CancellationToken cancellationToken = default)
        {
                var now = DateTimeOffset.UtcNow.Ticks;
                //TODO: split config and tokens, maybe use a class instead of tuple
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var requestState = await FetchStateFor(context, now);
                    var (remainingTokens, tokenLimit, lastRefill, refillPeriod, maxCapacity) = UpdateTokens(requestState, now);

                    if (remainingTokens < requestCount)
                        return AcquireResult.LimitRejected(TimeSpan.Zero, remainingTokens, 0);

                    remainingTokens -= requestCount;

                    tokensLeft[context] = (remainingTokens, tokenLimit, lastRefill, refillPeriod, maxCapacity);

                    if (remainingTokens < tokenLimit)
                        return AcquireResult.LimitAcquired( TimeSpan.FromTicks(refillPeriod * (tokenLimit - remainingTokens + requestCount)), remainingTokens, 0);

                    return AcquireResult.LimitAcquired(TimeSpan.Zero, remainingTokens, 0);
                }
                finally
                {
                    semaphore.Release();
                }
        }

        private (int RemainingToken, int TokenLimit, long LastRefill, long RefillPeriod, int MaxCapacity) UpdateTokens(
            (int RemainingTokens, int TokenLimit, long LastRefill, long RefillPeriod, int MaxCapacity) requestState, long now)
        {
            if (now >= requestState.LastRefill)
            {
                var tokensToAdd =  (now - requestState.LastRefill) / requestState.RefillPeriod;
                if (tokensToAdd >= 1)
                {
                    var nextRefillPeriod = requestState.LastRefill + tokensToAdd * requestState.RefillPeriod;
                    return ((int)Math.Min(requestState.RemainingTokens + tokensToAdd,  requestState.MaxCapacity), requestState.TokenLimit, nextRefillPeriod, requestState.RefillPeriod, requestState.MaxCapacity);
                }
            }
                
            return requestState;
        }
    }
}