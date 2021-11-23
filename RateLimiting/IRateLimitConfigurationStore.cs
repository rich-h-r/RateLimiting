using System.Threading.Tasks;

namespace RateLimiting
{
    public interface IRateLimitConfigurationStore<T>
    {
        Task<RateLimitConfiguration> GetConfigurationForAsync(T key);
    }
}