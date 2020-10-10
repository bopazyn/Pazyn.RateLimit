using System;
using Microsoft.AspNetCore.Builder;

namespace Pazyn.RateLimit
{
    public static class RateLimitExtensions
    {
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder) =>
            builder.UseMiddleware<EndpointRateLimitMiddleware>();

        public static IEndpointConventionBuilder AddRateLimit(this IEndpointConventionBuilder endpoint, Int32 limit, TimeSpan period)
        {
            endpoint.WithMetadata(new RateLimitMetadata(limit, period));
            return endpoint;
        }
    }
}