using System.Net;
using Api.Models;

namespace Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {

            try
            {
                await _next(httpContext);
                if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    await HandleUnauthorizedAsync(httpContext);
                }
                else if (httpContext.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    await HandleForbiddenAsync(httpContext);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";
            var problem = new CustomValidationProblemDetails
            {
                Success = false,
                Errors =
                [
                    "An unexpected error occurred. Please try again later."
                ],
                StatusCode = HttpStatusCode.InternalServerError,
            };
            await httpContext.Response.WriteAsJsonAsync(problem);
        }

        private Task HandleUnauthorizedAsync(HttpContext context)
        {
            var problem = new CustomValidationProblemDetails
            {
                Success = false,
                Errors =
                [
                    "You are not authorized to access this resource. Please log in and try again."
                ],
                StatusCode = HttpStatusCode.Unauthorized,
            };
            return context.Response.WriteAsJsonAsync(problem);
        }

        private Task HandleForbiddenAsync(HttpContext context)
        {
            var problem = new CustomValidationProblemDetails
            {
                Success = false,
                Errors =
              [
                  "You do not have permission to access this resource. If you need access, please contact the system administrator."
              ],
                StatusCode = HttpStatusCode.Forbidden,
            };
            return context.Response.WriteAsJsonAsync(problem);
        }
    }
}
