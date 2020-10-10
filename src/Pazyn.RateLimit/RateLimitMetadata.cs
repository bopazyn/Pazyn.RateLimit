using System;

namespace Pazyn.RateLimit
{
    internal class RateLimitMetadata
    {
        public Int32 Limit { get; }
        public TimeSpan Period { get; }

        public RateLimitMetadata(Int32 limit, TimeSpan period)
        {
            Limit = limit;
            Period = period;
        }

        public override String ToString() => $"Limit: {Limit}, Period: {Period}";
    }
}