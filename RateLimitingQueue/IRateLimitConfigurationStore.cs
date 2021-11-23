using System.Threading.Tasks;

namespace RateLimitingQueue
{
    public interface IRateLimitConfigurationStore<T>
    {
        Task<RateLimitConfiguration> GetConfigurationForAsync(T key);
    }
}