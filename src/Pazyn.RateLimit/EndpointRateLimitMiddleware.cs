using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Pazyn.RateLimit
{
    internal class EndpointRateLimitMiddleware
    {
        private RequestDelegate Next { get; }
        private IMemoryCache Cache { get; }

        public EndpointRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            Next = next;
            Cache = cache;
        }

        public Task Invoke(HttpContext httpContext)
        {
            var currentTime = DateTimeOffset.Now;
            return InvokePure(httpContext, currentTime);
        }

        public async Task InvokePure(HttpContext httpContext, DateTimeOffset currentTimestamp)
        {
            var endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
            {
                await Next(httpContext);
                return;
            }

            var rateLimitMetadatas = endpoint.Metadata.GetOrderedMetadata<RateLimitMetadata>();
            if (!rateLimitMetadatas.Any())
            {
                await Next(httpContext);
                return;
            }

            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
            var slidingWindowLogs = rateLimitMetadatas
                .Select(metadata =>
                {
                    var cacheKey = $"{nameof(EndpointRateLimitMiddleware)}:{clientIp}:{metadata.GetHashCode()}";
                    return Cache.GetOrCreate(cacheKey, entry =>
                    {
                        entry.SlidingExpiration = metadata.Period;
                        return new SlidingWindowLog(metadata);
                    });
                })
                .ToArray();

            foreach (var slidingWindowLog in slidingWindowLogs)
            {
                slidingWindowLog.ClearHistoricalRecords(currentTimestamp);
            }

            var retryAfter = 0;
            foreach (var slidingWindowLog in slidingWindowLogs)
            {
                if (slidingWindowLog.Count < slidingWindowLog.RateLimitMetadata.Limit)
                {
                    continue;
                }

                var queueRetryAfter = slidingWindowLog.FirstRecordTimestamp + slidingWindowLog.RateLimitMetadata.Period - currentTimestamp;
                retryAfter = Math.Max(retryAfter, (Int32)queueRetryAfter.TotalSeconds);
            }

            if (retryAfter > 0)
            {
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
                return;
            }

            foreach (var slidingWindowLog in slidingWindowLogs)
            {
                slidingWindowLog.Add(currentTimestamp);
            }
        }
    }
}