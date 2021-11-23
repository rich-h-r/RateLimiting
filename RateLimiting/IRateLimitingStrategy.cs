using System.Threading;
using System.Threading.Tasks;

namespace RateLimiting
{
    public interface IRateLimitingStrategy<T>
    {
        Task<AcquireResult> AcquireAsync(T context, int requestCount = 1, CancellationToken cancellationToken = default);
    }
}