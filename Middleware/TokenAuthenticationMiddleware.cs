namespace UserManagementAPI.Middleware;

/// <summary>
/// Validates a bearer token on every request before it reaches the
/// endpoints. Configure the expected token via "Authentication:ApiToken"
/// in appsettings.json (or an environment variable / secret store in
/// production — never commit a real secret to source control).
/// </summary>
public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenAuthenticationMiddleware> _logger;

    // Paths left open so Swagger's own UI/JSON can be browsed without a
    // token. The API endpoints under /users are NOT in this list.
    private static readonly string[] AnonymousPathPrefixes = { "/swagger" };

    public TokenAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<TokenAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (AnonymousPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var expectedToken = _configuration["Authentication:ApiToken"];
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            _logger.LogWarning("Authentication:ApiToken is not configured; rejecting request to {Path}", path);
            await WriteUnauthorized(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorized(context);
            return;
        }

        var providedToken = authHeader["Bearer ".Length..].Trim();
        if (!string.Equals(providedToken, expectedToken, StringComparison.Ordinal))
        {
            await WriteUnauthorized(context);
            return;
        }

        await _next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized." });
    }
}
