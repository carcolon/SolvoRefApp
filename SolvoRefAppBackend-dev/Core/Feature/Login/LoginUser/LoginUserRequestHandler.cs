using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Core.Contracts.Fabric;
using Core.Contracts.Identity;
using Core.Models.Global;
using Core.Models.Identity;
using Core.Security;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace Core.Feature.Login.LoginUser
{
    public class LoginUserRequestHandler : IRequestHandler<LoginUserRequest, string>
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IFabricService _fabricService;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;
        public LoginUserRequestHandler(IAuthService authService, IConfiguration configuration, IFabricService fabricService, IUserService userService, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _configuration = configuration;
            _fabricService = fabricService;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> Handle(LoginUserRequest request, CancellationToken cancellationToken)
        {
            var redirectUri =
                request.RedirectUri ??
                _configuration["AzureAd:RedirectUris:backend"];
            var clientId = _configuration["AzureAd:ClientId"];
            var clientSecret = _configuration["AzureAd:ClientSecret"];
            var authorityTenant =
                _configuration["AzureAd:AuthorityTenant"] ??
                _configuration["AzureAd:TenantId"] ??
                "organizations";

            var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithRedirectUri(redirectUri)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{authorityTenant}"))
            .Build();

            var result = await app.AcquireTokenByAuthorizationCode(
                ["https://graph.microsoft.com/User.Read"],
                request.Code)
                .ExecuteAsync();

            var idToken = result.IdToken;
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(idToken);

            if (IsUserInBlockedGroup(jwtToken))
            {
                throw new AuthAccessDeniedException("You are not authorized to Access the Referral App");
            }

            var email =
                jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
                jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ??
                jwtToken.Claims.FirstOrDefault(c => c.Type == "upn")?.Value;
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ??
                       jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ??
                       email?.Split('@').FirstOrDefault();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Unable to resolve user email from Microsoft token.");
            }
            var graphProfile = await TryGetGraphProfile(result.AccessToken, cancellationToken);
            var employeeId = graphProfile?.EmployeeId;
            var emailCandidates = GetEmailCandidates(email, graphProfile);
            var user = await GetUserByEmailCandidates(emailCandidates) ??
                await GetUserByEmployeeId(employeeId);
            LoginUserResponseDto data;
            if (user == null)
            {
                var extraUser = await TryGetExtraUserData(emailCandidates, employeeId) ?? new ExtraUser
                {
                    Email = email,
                    SolId = employeeId ?? string.Empty,
                    Status = "Active"
                };
                var registerData = new RegisterDtLoginRequest()
                {
                    Email = email,
                    FullName = name ?? email,
                    Country = extraUser.Country,
                    PayrollCompany = extraUser.PayrollCompany,
                    PersonalId = extraUser.PersonalId,
                    SolId = extraUser.SolId,
                    EmployeeId = employeeId ?? extraUser.SolId,
                    PayrollFrequencyClassification = extraUser.PayrollFrequencyClassification,
                    Status = extraUser.Status

                };
                data = await _authService.Register(registerData);
            }
            else
            {
                var extraUser = await TryGetExtraUserData(emailCandidates, employeeId);
                if (extraUser != null)
                {
                    user.Country = extraUser.Country;
                    user.PayrollCompany = extraUser.PayrollCompany;
                    user.PersonalId = extraUser.PersonalId;
                    user.SolId = extraUser.SolId;
                    user.PaymentFrequency = extraUser.PayrollFrequencyClassification;
                    user.Status = extraUser.Status;
                }

                if (!string.IsNullOrWhiteSpace(employeeId))
                {
                    user.EmployeeId = employeeId;
                }

                await _userService.UpdateUsers(user);

                var loginData = new LoginUserRequestDto()
                {
                    Email = email
                };
                data = await _authService.Login(loginData, user);
            }
            return data.Token;
        }

        private async Task<ExtraUser?> TryGetExtraUserData(IReadOnlyList<string> emailCandidates, string? employeeId)
        {
            Response<ExtraUser>? extraUserData = null;
            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                extraUserData = await _fabricService.GetExtraUserInformationBySolvoId(employeeId);
            }

            if (extraUserData == null || !HasResolvedExtraUser(extraUserData))
            {
                foreach (var email in emailCandidates)
                {
                    extraUserData = await _fabricService.GetExtraUserInformation(email);
                    if (HasResolvedExtraUser(extraUserData))
                    {
                        break;
                    }
                }
            }

            if (extraUserData == null || !HasResolvedExtraUser(extraUserData))
            {
                return null;
            }

            return extraUserData.Data;
        }

        private async Task<ApplicationUser?> GetUserByEmailCandidates(IReadOnlyList<string> emailCandidates)
        {
            foreach (var email in emailCandidates)
            {
                var user = await _authService.GetUserByEmail(email);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }

        private async Task<ApplicationUser?> GetUserByEmployeeId(string? employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return null;
            }

            return await _authService.GetUserByEmployeeIdOrSolId(employeeId);
        }

        private static bool HasResolvedExtraUser(Response<ExtraUser> response)
        {
            return response.Success &&
                response.Data != null &&
                (!string.IsNullOrWhiteSpace(response.Data.Country) ||
                 !string.IsNullOrWhiteSpace(response.Data.SolId) ||
                 !string.IsNullOrWhiteSpace(response.Data.PersonalId));
        }

        private async Task<GraphUserProfile?> TryGetGraphProfile(string accessToken, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://graph.microsoft.com/v1.0/me?$select=employeeId,mail,userPrincipalName");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var profile = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
            var employeeId = profile.RootElement.TryGetProperty("employeeId", out var property)
                ? property.GetString()?.Trim()
                : null;
            var mail = profile.RootElement.TryGetProperty("mail", out var mailProperty)
                ? mailProperty.GetString()?.Trim()
                : null;
            var userPrincipalName = profile.RootElement.TryGetProperty("userPrincipalName", out var upnProperty)
                ? upnProperty.GetString()?.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(employeeId) &&
                string.IsNullOrWhiteSpace(mail) &&
                string.IsNullOrWhiteSpace(userPrincipalName))
            {
                return null;
            }

            return new GraphUserProfile(employeeId, mail, userPrincipalName);
        }

        private static List<string> GetEmailCandidates(string tokenEmail, GraphUserProfile? graphProfile)
        {
            return new[]
            {
                tokenEmail,
                graphProfile?.Mail,
                graphProfile?.UserPrincipalName
            }
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => candidate!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        }

        private bool IsUserInBlockedGroup(JwtSecurityToken jwtToken)
        {
            var blockedGroupIds = _configuration.GetSection("AzureAd:BlockedGroupIds").Get<string[]>()
                ?? _configuration.GetSection("Auth:BlockedGroupIds").Get<string[]>()
                ?? [];

            if (blockedGroupIds.Length == 0)
            {
                return false;
            }

            var normalizedBlockedGroupIds = blockedGroupIds
                .Where(groupId => !string.IsNullOrWhiteSpace(groupId))
                .Select(groupId => groupId.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var tokenGroupIds = jwtToken.Claims
                .Where(claim => string.Equals(claim.Type, "groups", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(claim.Type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups", StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .Where(groupId => !string.IsNullOrWhiteSpace(groupId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return tokenGroupIds.Overlaps(normalizedBlockedGroupIds);
        }

        private sealed record GraphUserProfile(string? EmployeeId, string? Mail, string? UserPrincipalName);
    }
}
