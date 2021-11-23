using System;
using System.Threading.Tasks;

namespace RateLimitingQueue.ConfigurationStores
{
    /// <summary>
    /// just returns the default configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultRateLimitConfigurationStore<T> : IRateLimitConfigurationStore<T>
    {
        private readonly RateLimitConfiguration DefaultRateLimitConfiguration;

        public DefaultRateLimitConfigurationStore(TimeSpan window, int defaultNumberOfRequests, int maxRequest)
        {
            DefaultRateLimitConfiguration = new RateLimitConfiguration
                {TimeWindow = window, RequestsLimit = defaultNumberOfRequests, MaxCapacity = maxRequest};

        }

        public Task<RateLimitConfiguration> GetConfigurationForAsync(T key)
        {
            return Task.Run(() => DefaultRateLimitConfiguration);
        }
    }
}