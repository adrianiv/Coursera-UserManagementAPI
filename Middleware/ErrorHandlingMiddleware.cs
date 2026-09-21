namespace UserManagementAPI.Middleware;

/// <summary>
/// Catches any exception that escapes the rest of the pipeline and turns it
/// into a single, consistent JSON error shape instead of a raw stack trace
/// or a crashed process. Placed first in the pipeline so it can wrap every
/// other middleware and every endpoint.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            // If the response already started streaming, we can't rewrite
            // its headers/status — the best we can do is log and rethrow.
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
        }
    }
}
