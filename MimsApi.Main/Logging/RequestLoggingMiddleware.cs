using System.Diagnostics;
using Serilog;

namespace MimsApi.Main.Logging
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            // Log request details
            LogRequest(request);

            // Copy response body stream so we can log it
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, 
                        "Exception occurred during request. Method: {Method}, Path: {Path}, Duration: {Duration}ms",
                        request.Method, request.Path, stopwatch.ElapsedMilliseconds);
                    throw;
                }

                stopwatch.Stop();

                // Log response details
                LogResponse(context.Response, request, stopwatch.ElapsedMilliseconds);

                // Seek to the beginning before copying response to original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private void LogRequest(HttpRequest request)
        {
            var userId = GetUserIdFromContext(request.HttpContext);
            
            _logger.LogInformation(
                "HTTP Request: {Method} {Path} | Query: {QueryString} | UserId: {UserId}",
                request.Method,
                request.Path,
                request.QueryString.ToString(),
                userId ?? "Anonymous");
        }

        private void LogResponse(HttpResponse response, HttpRequest request, long duration)
        {
            var userId = GetUserIdFromContext(request.HttpContext);
            var statusCode = response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error : 
                        statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(level,
                "HTTP Response: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | UserId: {UserId}",
                request.Method,
                request.Path,
                statusCode,
                duration,
                userId ?? "Anonymous");
        }

        private string? GetUserIdFromContext(HttpContext context)
        {
            return context.User?.FindFirst("sub")?.Value ??
                   context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ??
                   context.Items["UserId"]?.ToString();
        }
    }

    /// <summary>
    /// Extension method to add request logging middleware
    /// </summary>
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
