using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Api.Core.Policies;

/// <summary>
/// Rate limiter policy to limit by user agent
/// </summary>
public class UserAgentPolicy : IRateLimiterPolicy<string>
{
    private ILogger<UserAgentPolicy> logger;

    public UserAgentPolicy(ILogger<UserAgentPolicy> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers.UserAgent[0];

        // The fixed window limiter limits requests within a specific window
        return RateLimitPartition.GetFixedWindowLimiter(userAgent,
                                                        (_) => new FixedWindowRateLimiterOptions
                                                        {
                                                            Window = TimeSpan.FromSeconds(1),

                                                            PermitLimit = 2,
                                                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                                            QueueLimit = 1
                                                        });
    }

    /// <inheritdoc />
    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => (context, lease) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        logger.LogDebug("Request rejected");
        return ValueTask.CompletedTask;
    };
}