using System;

namespace RateLimitingQueue
{
    public class AcquireResult
    {
        private AcquireResult(bool acquired, TimeSpan waitTime, int requestLimit, int requestsMadeInWindow,
            TimeSpan window) 
        {
            Acquired = acquired;
            WaitTime = waitTime; 
            RemainingRequestInWindow = requestLimit - requestsMadeInWindow;
            RequestsMadeInWindow = requestsMadeInWindow;
            Window = window;
        }
        
        public bool Acquired { get; }
        public TimeSpan WaitTime { get; }
        public int RemainingRequestInWindow { get; }
        public int RequestsMadeInWindow { get; }

        public static AcquireResult LimitAcquired(TimeSpan window, int requestLimit, int requestsMadeInWindow) 
            => new AcquireResult(true, window, requestLimit, requestsMadeInWindow, window);
        public static AcquireResult LimitRejected(TimeSpan window, int requestLimit, int requestsMadeInWindow) 
            => new AcquireResult(false, window, requestLimit, requestsMadeInWindow, window);

        public TimeSpan Window { get; }

        public static AcquireResult LimitRejected(RateLimitConfiguration userConfiguration, int processedRequestsCount, TimeSpan firstRequestAge)
            => new AcquireResult(false, userConfiguration.TimeWindow-firstRequestAge, userConfiguration.RequestsLimit, processedRequestsCount, userConfiguration.TimeWindow);
        
        public static AcquireResult LimitAcquired(RateLimitConfiguration userConfiguration, int processedRequestsCount)
            => new AcquireResult(true, TimeSpan.Zero, userConfiguration.RequestsLimit, processedRequestsCount, userConfiguration.TimeWindow);

        public static implicit operator bool(AcquireResult result)
        {
            return result.Acquired;
        }

        public static AcquireResult NoOp => new AcquireResult(true, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
    }
}