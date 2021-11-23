using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimitingQueue.LimitingStrategies
{
    public class MovingWindow<T> : IRateLimitingStrategy<T>
    {
        private readonly IRateLimitConfigurationStore<T> configurationStore;
        //TODO: should the store hold this?
        // would allow us to push onto redis, and be distributed at the moment we handle the logic
        private readonly Dictionary<T, LinkedList<long>> processedRequestsStore = new Dictionary<T, LinkedList<long>>();
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public MovingWindow(IRateLimitConfigurationStore<T> rateLimitConfigurationStore)
        {
            configurationStore = rateLimitConfigurationStore;
        }
        
        public async Task<AcquireResult> AcquireAsync(T context, int requestCount = 1, CancellationToken cancellationToken = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            
            var now = DateTimeOffset.Now;
            var userConfiguration = await configurationStore.GetConfigurationForAsync(context);

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var processedRequests = GetProcessedRequestsFor(context);

                //remove all old requests
                while (processedRequests.First?.Value < now.Subtract(userConfiguration.TimeWindow).Ticks)
                    processedRequests.RemoveFirst();

                if (processedRequests.Count >= userConfiguration.RequestsLimit)
                    return AcquireResult.LimitRejected(userConfiguration, processedRequests.Count,
                        TimeSpan.FromTicks(now.Ticks - processedRequests.First?.Value ?? 0L));

                processedRequests.AddLast(now.Ticks);
                return AcquireResult.LimitAcquired(userConfiguration, processedRequests.Count);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private LinkedList<long> GetProcessedRequestsFor(T userConfiguration)
        {
            if (processedRequestsStore.TryGetValue(userConfiguration, out var requests))
                return requests;

            var emptyRequestList = new LinkedList<long>();
            
            processedRequestsStore.Add(userConfiguration, emptyRequestList);
            return emptyRequestList;
        }
    }
}