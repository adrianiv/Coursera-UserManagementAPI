namespace UserManagementAPI.Middleware;

/// Logs the HTTP method, request path, and the resulting response status
/// code for every request 

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;

        await _next(context);

        _logger.LogInformation("{Method} {Path} -> {StatusCode}", method, path, context.Response.StatusCode);
    }
}
