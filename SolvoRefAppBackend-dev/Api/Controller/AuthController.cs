using Core.Feature.Login.LoginUser;
using Core.Feature.Login.ValidateToken;
using Core.Models.Global;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Core.Models.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Core.Security;
using Core.Contracts.Identity;

namespace Api.Controller
{
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(
            IMediator mediator,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger,
            IAuthService authService)
        {
            _mediator = mediator;
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
            _authService = authService;
        }

        [HttpGet("microsoft/callback")]
        [Produces<string>]
        public async Task<ActionResult<string>> Login(string code)
        {
            var backendRedirectUri =
                _configuration["AzureAd:RedirectUris:Backend"] ??
                _configuration["AzureAd:RedirectUris:backend"];

            _logger.LogInformation(
                "Auth callback start. HasCode: {HasCode}; RedirectUri: {RedirectUri}; UserAgent: {UserAgent}",
                !string.IsNullOrWhiteSpace(code),
                backendRedirectUri,
                Request.Headers.UserAgent.ToString());

            var token = await _mediator.Send(new LoginUserRequest(code, backendRedirectUri));
            var redirectUriFront =
                _configuration["AzureAd:RedirectUris:Frontend"] ??
                _configuration["AzureAd:RedirectUris:frontend"] ??
                _configuration["frontRedirect"] ??
                "/";

            AppendAuthCookie(token, GetAuthCookieExpires(), redirectUriFront);

            _logger.LogInformation(
                "Auth callback success. Redirecting to frontend: {RedirectUriFront}",
                redirectUriFront);

            return Redirect(redirectUriFront);
        }

        [HttpPost("microsoft/exchange")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> ExchangeCode([FromBody] MicrosoftExchangeRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
            {
                _logger.LogWarning(
                    "Auth exchange rejected. Missing code. UserAgent: {UserAgent}",
                    Request.Headers.UserAgent.ToString());

                return BadRequest(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Errors = ["Authorization code is required."]
                });
            }

            var frontendRedirectUri = ResolveFrontendRedirectUri(request.RedirectUri);

            _logger.LogInformation(
                "Auth exchange start. HasCode: {HasCode}; RedirectUri: {RedirectUri}; UserAgent: {UserAgent}",
                !string.IsNullOrWhiteSpace(request.Code),
                frontendRedirectUri,
                Request.Headers.UserAgent.ToString());

            string token;
            try
            {
                token = await _mediator.Send(new LoginUserRequest(request.Code, frontendRedirectUri));
            }
            catch (AuthAccessDeniedException ex)
            {
                _logger.LogWarning(
                    "Auth exchange blocked by group membership. UserAgent: {UserAgent}",
                    Request.Headers.UserAgent.ToString());

                return StatusCode(StatusCodes.Status403Forbidden, new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.Forbidden,
                    Errors = [ex.Message]
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Auth exchange could not complete login for user. UserAgent: {UserAgent}",
                    Request.Headers.UserAgent.ToString());

                return BadRequest(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    Errors = [ex.Message]
                });
            }

            _logger.LogInformation("Auth exchange success.");
            var expires = GetAuthCookieExpires();
            AppendAuthCookie(token, expires, frontendRedirectUri);

            return new Response<object>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Data = CreateAuthResponseData(token, expires)
            };
        }

        [HttpPost("validatetoken")]
        [Produces<Response<bool>>]
        public async Task<ActionResult<Response<bool>>> ValidateToken([FromBody] ValidateTokenRequestDto? request)
        {
            var token = request?.Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                Request.Cookies.TryGetValue("auth_token", out token);
            }
            return await _mediator.Send(new ValidateTokenRequest(token ?? string.Empty));
        }

        [Authorize]
        [HttpPost("refresh")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> Refresh()
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    Errors = ["Invalid session."]
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Unauthorized(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    Errors = ["Invalid session."]
                });
            }

            var token = await _authService.GenerateToken(user);
            var expires = GetAuthCookieExpires();
            var frontendRedirectUri =
                _configuration["AzureAd:RedirectUris:Frontend"] ??
                _configuration["AzureAd:RedirectUris:frontend"] ??
                _configuration["frontRedirect"];
            AppendAuthCookie(token, expires, frontendRedirectUri);

            return new Response<object>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Data = CreateAuthResponseData(token, expires)
            };
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst("uid")?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is not null)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                }
            }

            AppendAuthCookie(string.Empty, DateTimeOffset.UtcNow.AddDays(-1));

            return Ok();
        }

        [Authorize]
        [HttpGet("csrf")]
        [Produces<Response<object>>]
        public ActionResult<Response<object>> GetCsrfToken()
        {
            if (!Request.Cookies.TryGetValue("auth_token", out var authToken) ||
                string.IsNullOrWhiteSpace(authToken))
            {
                return Unauthorized(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    Errors = ["Invalid session."]
                });
            }

            return new Response<object>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Data = new
                {
                    csrf = CreateCsrfToken(authToken)
                }
            };
        }

        [Authorize]
        [HttpGet("me")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> Me()
        {
            var name = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value ??
                       User.FindFirst("name")?.Value ??
                       User.FindFirst(ClaimTypes.Name)?.Value ??
                       User.Identity?.Name ??
                       string.Empty;

            var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
                        User.FindFirst("emailuser")?.Value ??
                        User.FindFirst("email")?.Value ??
                        User.FindFirst(ClaimTypes.Email)?.Value ??
                        User.FindFirst("preferred_username")?.Value ??
                        User.FindFirst("upn")?.Value ??
                        User.FindFirst("unique_name")?.Value ??
                        string.Empty;

            var roles = User.Claims
                .Where(c => c.Type == "roles" || c.Type == "role" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var userId = User.FindFirst("uid")?.Value;
            var isSolvoPartner = false;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                isSolvoPartner = string.Equals(user?.SolvoPartnerStatus, "Active", StringComparison.OrdinalIgnoreCase);
            }

            _logger.LogInformation(
                "Auth me success. Email: {Email}; Roles: {Roles}; UserAgent: {UserAgent}",
                email,
                string.Join(",", roles),
                Request.Headers.UserAgent.ToString());

            return new Response<object>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Data = new
                {
                name,
                email,
                roles,
                isSolvoPartner
                }
            };
        }

        [Authorize]
        [HttpGet("diagnostics/employee-id")]
        [Produces<Response<object>>]
        public async Task<ActionResult<Response<object>>> GetEmployeeIdDiagnostic()
        {
            var userId = User.FindFirst("uid")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.Unauthorized,
                    Errors = ["Invalid session."]
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound(new Response<object>
                {
                    Success = false,
                    StatusCode = HttpStatusCode.NotFound,
                    Errors = ["The authenticated user was not found in the application database."]
                });
            }

            return new Response<object>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Data = new
                {
                    user.Email,
                    user.EmployeeId,
                    HasEmployeeId = !string.IsNullOrWhiteSpace(user.EmployeeId),
                    Source = "AspNetUsers"
                }
            };
        }

        private void AppendAuthCookie(string token, DateTimeOffset expires, string? frontendRedirectUri = null)
        {
            Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = ResolveAuthCookieSameSite(frontendRedirectUri),
                Path = "/",
                IsEssential = true,
                Expires = expires
            });
        }

        private static object CreateAuthResponseData(string token, DateTimeOffset expires)
        {
            return new
            {
                accessToken = token,
                expiresAt = expires
            };
        }

        private string CreateCsrfToken(string authToken)
        {
            return CsrfTokenProtector.Create(authToken, _configuration["JwtSetting:Key"] ?? string.Empty);
        }

        private DateTimeOffset GetAuthCookieExpires()
        {
            var durationInMinutes = 60d;
            if (double.TryParse(_configuration["JwtSetting:DurationInMinutes"], out var configuredDuration) &&
                configuredDuration > 0)
            {
                durationInMinutes = configuredDuration;
            }

            return DateTimeOffset.UtcNow.AddMinutes(durationInMinutes);
        }

        private SameSiteMode ResolveAuthCookieSameSite(string? frontendRedirectUri)
        {
            var configuredSameSite = _configuration["Auth:CookieSameSite"];
            if (Enum.TryParse<SameSiteMode>(configuredSameSite, ignoreCase: true, out var sameSiteMode))
            {
                return sameSiteMode;
            }

            if (Uri.TryCreate(frontendRedirectUri, UriKind.Absolute, out var frontendUri) &&
                !string.Equals(frontendUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return SameSiteMode.None;
            }

            return SameSiteMode.Strict;
        }

        private string? ResolveFrontendRedirectUri(string? requestedRedirectUri)
        {
            var configuredRedirectUri =
                _configuration["AzureAd:RedirectUris:Frontend"] ??
                _configuration["AzureAd:RedirectUris:frontend"] ??
                _configuration["frontRedirect"];

            if (!Uri.TryCreate(requestedRedirectUri, UriKind.Absolute, out var requestedUri))
            {
                return configuredRedirectUri;
            }

            var requestOrigin = Request.Headers.Origin.ToString();
            if (!Uri.TryCreate(requestOrigin, UriKind.Absolute, out var originUri))
            {
                return configuredRedirectUri;
            }

            if (!string.Equals(requestedUri.Scheme, originUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(requestedUri.Host, originUri.Host, StringComparison.OrdinalIgnoreCase) ||
                requestedUri.Port != originUri.Port)
            {
                _logger.LogWarning(
                    "Auth exchange ignored redirect URI because it does not match Origin. RequestedRedirectUri: {RequestedRedirectUri}; Origin: {Origin}",
                    requestedRedirectUri,
                    requestOrigin);

                return configuredRedirectUri;
            }

            return requestedUri.ToString();
        }
    }
}
