using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Core.Models.Global;
using Core.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controller
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/users")]
    [EnableRateLimiting("admin-content-write")]
    public class AdminUsersController : ControllerBase
    {
        private const string ActiveStatus = "Active";
        private const string InactiveStatus = "Inactive";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Produces<Response<List<AdminUserDto>>>]
        public async Task<ActionResult<Response<List<AdminUserDto>>>> GetAdmins()
        {
            await EnsureAdminRole();
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var data = admins
                .OrderByDescending(IsActiveAdmin)
                .ThenBy(user => user.Email)
                .Select(MapAdminUser)
                .ToList();

            return Response<List<AdminUserDto>>.SuccessResponse(data, HttpStatusCode.OK);
        }

        [HttpPost]
        [Produces<Response<AdminUserDto>>]
        public async Task<ActionResult<Response<AdminUserDto>>> CreateAdmin([FromBody] CreateAdminUserRequest request)
        {
            var email = NormalizeEmail(request.Email);
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(Response<AdminUserDto>.ErrorResponse(["A valid email is required."], HttpStatusCode.BadRequest));
            }

            await EnsureRole("User");
            await EnsureRole("Admin");

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FullName = BuildDisplayNameFromEmail(email),
                    EmailConfirmed = true,
                    Status = ActiveStatus
                };

                var createUserResult = await _userManager.CreateAsync(user);
                if (!createUserResult.Succeeded)
                {
                    return BadRequest(Response<AdminUserDto>.ErrorResponse(ToErrors(createUserResult), HttpStatusCode.BadRequest));
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var addUserRoleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!addUserRoleResult.Succeeded)
                {
                    return BadRequest(Response<AdminUserDto>.ErrorResponse(ToErrors(addUserRoleResult), HttpStatusCode.BadRequest));
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var addAdminRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addAdminRoleResult.Succeeded)
                {
                    return BadRequest(Response<AdminUserDto>.ErrorResponse(ToErrors(addAdminRoleResult), HttpStatusCode.BadRequest));
                }
            }

            user.Status = ActiveStatus;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(Response<AdminUserDto>.ErrorResponse(ToErrors(updateResult), HttpStatusCode.BadRequest));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            return Response<AdminUserDto>.SuccessResponse(MapAdminUser(user), HttpStatusCode.OK);
        }

        [HttpPost("{id}/activate")]
        [Produces<Response<AdminUserDto>>]
        public async Task<ActionResult<Response<AdminUserDto>>> ActivateAdmin(string id)
        {
            var user = await FindAdminUser(id);
            if (user is null)
            {
                return NotFound(Response<AdminUserDto>.ErrorResponse(["Admin user was not found."], HttpStatusCode.NotFound));
            }

            user.Status = ActiveStatus;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(Response<AdminUserDto>.ErrorResponse(ToErrors(result), HttpStatusCode.BadRequest));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            return Response<AdminUserDto>.SuccessResponse(MapAdminUser(user), HttpStatusCode.OK);
        }

        [HttpPost("{id}/deactivate")]
        [Produces<Response<AdminUserDto>>]
        public async Task<ActionResult<Response<AdminUserDto>>> DeactivateAdmin(string id)
        {
            var user = await FindAdminUser(id);
            if (user is null)
            {
                return NotFound(Response<AdminUserDto>.ErrorResponse(["Admin user was not found."], HttpStatusCode.NotFound));
            }

            var validation = await ValidateCanDisableAdmin(user);
            if (!validation.Success)
            {
                return BadRequest(Response<AdminUserDto>.ErrorResponse(validation.Errors, HttpStatusCode.BadRequest));
            }

            user.Status = InactiveStatus;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(Response<AdminUserDto>.ErrorResponse(ToErrors(result), HttpStatusCode.BadRequest));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            return Response<AdminUserDto>.SuccessResponse(MapAdminUser(user), HttpStatusCode.OK);
        }

        [HttpDelete("{id}")]
        [Produces<Response<bool>>]
        public async Task<ActionResult<Response<bool>>> RemoveAdmin(string id)
        {
            var user = await FindAdminUser(id);
            if (user is null)
            {
                return NotFound(Response<bool>.ErrorResponse(["Admin user was not found."], HttpStatusCode.NotFound));
            }

            var validation = await ValidateCanDisableAdmin(user);
            if (!validation.Success)
            {
                return BadRequest(Response<bool>.ErrorResponse(validation.Errors, HttpStatusCode.BadRequest));
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!result.Succeeded)
            {
                return BadRequest(Response<bool>.ErrorResponse(ToErrors(result), HttpStatusCode.BadRequest));
            }

            await _userManager.UpdateSecurityStampAsync(user);

            return Response<bool>.SuccessResponse(true, HttpStatusCode.OK);
        }

        private async Task<ApplicationUser?> FindAdminUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return null;
            }

            return user;
        }

        private async Task<Response<bool>> ValidateCanDisableAdmin(ApplicationUser user)
        {
            var currentUserId = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.Equals(currentUserId, user.Id, StringComparison.Ordinal))
            {
                return Response<bool>.ErrorResponse(["You cannot remove your own admin access."], HttpStatusCode.BadRequest);
            }

            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var activeAdmins = admins.Count(admin =>
                !string.Equals(admin.Id, user.Id, StringComparison.Ordinal) &&
                IsActiveAdmin(admin));

            if (activeAdmins == 0)
            {
                return Response<bool>.ErrorResponse(["At least one active admin is required."], HttpStatusCode.BadRequest);
            }

            return Response<bool>.SuccessResponse(true, HttpStatusCode.OK);
        }

        private static AdminUserDto MapAdminUser(ApplicationUser user)
        {
            return new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Status = user.Status,
                IsActive = IsActiveAdmin(user)
            };
        }

        private async Task EnsureAdminRole()
        {
            await EnsureRole("Admin");
        }

        private async Task EnsureRole(string role)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                return;
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create {role} role: {string.Join(", ", ToErrors(result))}");
            }
        }

        private static bool IsActiveAdmin(ApplicationUser user)
        {
            return !string.Equals(user.Status, InactiveStatus, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeEmail(string? email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static List<string> ToErrors(IdentityResult result)
        {
            return result.Errors.Select(error => error.Description).ToList();
        }

        private static string BuildDisplayNameFromEmail(string email)
        {
            var localPart = email.Split('@', StringSplitOptions.RemoveEmptyEntries)[0];
            return string.Join(' ', localPart
                .Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        }
    }

    public class CreateAdminUserRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
