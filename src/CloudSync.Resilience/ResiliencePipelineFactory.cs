using System.Net;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace CloudSync.Resilience;

public static class ResiliencePipelineFactory
{
    /// <summary>
    /// Creates a standard resilience policy for HTTP calls, handling transient errors (503) 
    /// and Too Many Requests (429) using exponential backoff.
    /// </summary>
    public static AsyncRetryPolicy<HttpResponseMessage> CreateHttpRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // In a complete implementation, you'd log here via ILogger.
                    // E.g. "Delaying for {timespan} before making retry {retryAttempt}"
                });
    }

    /// <summary>
    /// Creates a fallback or specific policy for Drive API errors.
    /// </summary>
    public static AsyncRetryPolicy CreateDriveApiRetryPolicy()
    {
        return Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
            );
    }
}
