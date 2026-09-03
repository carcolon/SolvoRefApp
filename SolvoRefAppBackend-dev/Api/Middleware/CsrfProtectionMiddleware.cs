using Core.Security;

namespace Api.Middleware
{
    public class CsrfProtectionMiddleware
    {
        private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

        private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/microsoft/exchange"
        };

        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public CsrfProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!UnsafeMethods.Contains(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (ExcludedPaths.Contains(path))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Cookies.TryGetValue("auth_token", out var authToken) ||
                string.IsNullOrWhiteSpace(authToken))
            {
                await _next(context);
                return;
            }

            // If request uses bearer token explicitly, server-side JWT validation remains the primary control.
            var authHeader = context.Request.Headers.Authorization.ToString();
            var hasBearerToken = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            if (hasBearerToken)
            {
                await _next(context);
                return;
            }

            var signingKey = _configuration["JwtSetting:Key"] ?? string.Empty;
            if (!context.Request.Headers.TryGetValue("X-CSRF-Token", out var csrfToken) ||
                !CsrfTokenProtector.Validate(csrfToken.ToString(), authToken, signingKey))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    errors = new[] { "Invalid CSRF token." },
                    data = (object?)null
                });
                return;
            }

            await _next(context);
        }
    }
}
