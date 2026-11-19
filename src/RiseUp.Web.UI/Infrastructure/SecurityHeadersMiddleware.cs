namespace RiseUp.Web.UI.Infrastructure;

/// <summary>Security headers + correlation id on every response (NFR-13, NFR-17).</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h["X-Correlation-ID"] = correlationId;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob:; media-src 'self' blob:; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; script-src 'self'; connect-src 'self'; frame-ancestors 'none'; form-action 'self'; base-uri 'self'";
            if (context.Request.Path.StartsWithSegments("/admin") || context.User.Identity?.IsAuthenticated == true)
                h["X-Robots-Tag"] = "noindex, nofollow";
            return Task.CompletedTask;
        });
        using (context.RequestServices.GetRequiredService<ILogger<SecurityHeadersMiddleware>>().BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            await next(context);
    }
}
