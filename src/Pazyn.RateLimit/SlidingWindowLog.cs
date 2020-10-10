using System;
using System.Collections.Concurrent;

namespace Pazyn.RateLimit
{
    internal class SlidingWindowLog
    {
        public RateLimitMetadata RateLimitMetadata { get; }
        public DateTimeOffset FirstRecordTimestamp { get; private set; }
        private ConcurrentQueue<DateTimeOffset> Queue { get; } = new ConcurrentQueue<DateTimeOffset>();
        public Int32 Count => Queue.Count;

        public SlidingWindowLog(RateLimitMetadata rateLimitMetadata)
        {
            RateLimitMetadata = rateLimitMetadata;
        }

        public void ClearHistoricalRecords(DateTimeOffset timestamp)
        {
            while (Queue.TryPeek(out var first) && first <= timestamp - RateLimitMetadata.Period)
            {
                Queue.TryDequeue(out _);
            }

            FirstRecordTimestamp = Queue.TryPeek(out var t) ? t : timestamp;
        }

        public void Add(in DateTimeOffset timestamp)
        {
            Queue.Enqueue(timestamp);
        }
    }
}
