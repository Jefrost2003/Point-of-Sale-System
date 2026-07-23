using Microsoft.AspNetCore.Http;

namespace IT15_INATO_POS.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers to prevent common attacks
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer-when-downgrade");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
            context.Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");

            // ✅ CSP - Allow images from ANY source (absolute URLs from internet)
#pragma warning disable S7039
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self' https: http:; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://www.google.com https://www.gstatic.com https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " +
                "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
                "img-src 'self' data: https: http: blob: *; " +  // ✅ Allow images from ANY URL
                "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                "connect-src 'self' https:; " +
                "frame-src https://www.google.com https://recaptcha.google.com/recaptcha/; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'; " +
                "frame-ancestors 'none'");
#pragma warning restore S7039

            await _next(context);
        }
    }
}