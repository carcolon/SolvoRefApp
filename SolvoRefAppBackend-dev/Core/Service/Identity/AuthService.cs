using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Core.Contracts.Identity;
using Core.Feature.Login.LoginUser;
using Core.Models.Identity;
using Core.Models.Global;
using Microsoft.EntityFrameworkCore;

namespace Core.Services.Identity
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSetting _jwtSetting;
        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IOptions<JwtSetting> jwtSetting)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtSetting = jwtSetting.Value;
        }

        public async Task<LoginUserResponseDto> Login(LoginUserRequestDto request, ApplicationUser user)
        {

            var roles = await _userManager.GetRolesAsync(user);
            roles = FilterInactiveAdminRole(user, roles);
            if (!roles.Contains("User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
                roles.Add("User");
            }
            JwtSecurityToken jwtSecurityToken = await GenerateJwtSecurityToken(user);
            var authResponse = new LoginUserResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Email = user.Email ?? "",
                Name = $"{user.FullName}",
                Roles = [.. roles],
            };
            return authResponse;
        }

        public async Task<LoginUserResponseDto> Register(RegisterDtLoginRequest loginData)
        {
            var user = new ApplicationUser
            {
                Email = loginData.Email.ToLower(),
                UserName = Helpers.HelpersUtils.ReplaceAccentsAndSpecialChars($"{loginData.FullName}".ToUpper()).Replace(" ", "."),
                FullName = loginData.FullName,
                PersonalId = loginData.PersonalId,
                PayrollCompany = loginData.PayrollCompany,
                Country = loginData.Country,
                SolId = loginData.SolId,
                EmployeeId = loginData.EmployeeId,
                PaymentFrequency = loginData.PayrollFrequencyClassification,
                Status = string.IsNullOrWhiteSpace(loginData.Status) ? "Active" : loginData.Status,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                var rolesList = new List<string>
                {
                    "User"
                };
                await _userManager.AddToRoleAsync(user, "User");
                JwtSecurityToken jwtSecurityToken = await GenerateJwtSecurityToken(user);
                var response = new LoginUserResponseDto
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                    Email = user.Email ?? "",
                    Name = $"{user.FullName}",
                    Roles = rolesList
                };
                return response;
            }
            else
            {
                throw new ArgumentException(result.Errors.ToList()[0].Description);
            }
        }

        public async Task<ApplicationUser?> GetUserByEmail(string email)
        {
            return await _userManager.FindByEmailAsync(email.ToLower());

        }

        public async Task<ApplicationUser?> GetUserByEmployeeIdOrSolId(string employeeId)
        {
            var normalizedEmployeeId = (employeeId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmployeeId))
            {
                return null;
            }

            return await _userManager.Users.FirstOrDefaultAsync(user =>
                user.EmployeeId == normalizedEmployeeId ||
                user.SolId == normalizedEmployeeId);
        }

        public async Task<string> GenerateToken(ApplicationUser user)
        {
            var jwtSecurityToken = await GenerateJwtSecurityToken(user);
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }

        private async Task<JwtSecurityToken> GenerateJwtSecurityToken(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            roles = FilterInactiveAdminRole(user, roles);

            var rolesClaims = roles.Select(x => new Claim("roles", x)).ToList();

            var claims = new[]{
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Name, $"{user.FullName}"),
                new Claim("uid",user.Id),
                new Claim("emailuser",user.Email ?? ""),
                new Claim("sstamp", user.SecurityStamp ?? string.Empty),
                new Claim("solvo_partner", IsActiveSolvoPartner(user).ToString().ToLowerInvariant())
            }
            .Union(userClaims)
            .Union(rolesClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSetting.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwtSetting.Issuer,
                audience: _jwtSetting.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_jwtSetting.DurationInMinutes)),
                signingCredentials: signingCredentials
            );
            return jwtSecurityToken;
        }

        private static IList<string> FilterInactiveAdminRole(ApplicationUser user, IList<string> roles)
        {
            if (!string.Equals(user.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
            {
                return roles;
            }

            return roles
                .Where(role => !string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static bool IsActiveSolvoPartner(ApplicationUser user)
        {
            return string.Equals(user.SolvoPartnerStatus, "Active", StringComparison.OrdinalIgnoreCase);
        }

        public async Task DeleteUser(ApplicationUser user)
        {
            await _userManager.DeleteAsync(user);
        }

        public async Task<bool> ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtSetting.Issuer,
                    ValidAudience = _jwtSetting.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSetting.Key)),
                    ClockSkew = TimeSpan.Zero
                };
                var tokenValidatedResult = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);
                return tokenValidatedResult.IsValid;
            }
            catch
            {
                return false;
            }
        }
    }
}
