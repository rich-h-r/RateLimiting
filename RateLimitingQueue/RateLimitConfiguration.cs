using System;

namespace RateLimitingQueue
{
    public class RateLimitConfiguration
    {
        /// <summary>
        /// For moving window, the size of the window
        /// For Token bucket the period that tokens are added
        /// </summary>
        public TimeSpan TimeWindow { get; set; }
        /// <summary>
        /// Initial request Limit
        /// </summary>
        public int RequestsLimit { get; set; }
        /// <summary>
        /// Maximum capacity of the the bucket
        /// Used for replenishing the token and leaky bucket strategies
        /// </summary>
        public int MaxCapacity { get; set; }
    }
}