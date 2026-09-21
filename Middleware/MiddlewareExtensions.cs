namespace UserManagementAPI.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ErrorHandlingMiddleware>();

    public static IApplicationBuilder UseTokenAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<TokenAuthenticationMiddleware>();

    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
}
