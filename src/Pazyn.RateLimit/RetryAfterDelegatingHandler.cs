using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Pazyn.RateLimit
{
    public class RetryAfterDelegatingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            while (true)
            {
                response = await base.SendAsync(request, cancellationToken);
                if (response.Headers.RetryAfter == null)
                {
                    break;
                }

                if (response.Headers.RetryAfter.Delta != null)
                {
                    await Task.Delay(response.Headers.RetryAfter.Delta.Value, cancellationToken);
                }

                if (response.Headers.RetryAfter.Date != null)
                {
                    await Task.Delay(response.Headers.RetryAfter.Date.Value - DateTimeOffset.Now, cancellationToken);
                }
            }

            return response;
        }
    }
}