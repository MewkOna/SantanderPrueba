using System.Collections.Concurrent;

namespace HackerNewsBestStories.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clientRequests = new();
    private static readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);
    private const int _maxRequestsPerWindow = 100;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var now = DateTime.UtcNow;

        var clientInfo = _clientRequests.AddOrUpdate(clientId,
            new ClientRequestInfo { FirstRequestTime = now, RequestCount = 1 },
            (key, existingInfo) =>
            {
                if (now - existingInfo.FirstRequestTime > _timeWindow)
                {
                    // Reset window
                    return new ClientRequestInfo { FirstRequestTime = now, RequestCount = 1 };
                }

                existingInfo.RequestCount++;
                return existingInfo;
            });

        if (clientInfo.RequestCount > _maxRequestsPerWindow)
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = _timeWindow.TotalSeconds.ToString();
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Use IP address or API key if available
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class ClientRequestInfo
    {
        public DateTime FirstRequestTime { get; set; }
        public int RequestCount { get; set; }
    }
}