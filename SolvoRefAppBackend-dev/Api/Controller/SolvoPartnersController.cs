using System.ComponentModel.DataAnnotations;
using System.Net;
using Core.Models.Global;
using Core.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Api.Controller
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/solvo-partners")]
    [EnableRateLimiting("admin-content-write")]
    public class SolvoPartnersController : ControllerBase
    {
        private const string ActiveStatus = "Active";
        private const string InactiveStatus = "Inactive";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SolvoPartnersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Produces<Response<List<SolvoPartnerDto>>>]
        public async Task<ActionResult<Response<List<SolvoPartnerDto>>>> GetPartners()
        {
            var data = await _userManager.Users
                .Where(user => user.SolvoPartnerStatus == ActiveStatus || user.SolvoPartnerStatus == InactiveStatus)
                .OrderByDescending(user => user.SolvoPartnerStatus == ActiveStatus)
                .ThenBy(user => user.Email)
                .Select(user => new SolvoPartnerDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    Status = user.SolvoPartnerStatus,
                    IsActive = user.SolvoPartnerStatus == ActiveStatus
                })
                .ToListAsync();

            return Response<List<SolvoPartnerDto>>.SuccessResponse(data, HttpStatusCode.OK);
        }

        [HttpPost]
        [Produces<Response<SolvoPartnerDto>>]
        public async Task<ActionResult<Response<SolvoPartnerDto>>> CreatePartner([FromBody] CreateSolvoPartnerRequest request)
        {
            var email = NormalizeEmail(request.Email);
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                return BadRequest(Response<SolvoPartnerDto>.ErrorResponse(["A valid email is required."], HttpStatusCode.BadRequest));
            }

            await EnsureRole("User");

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FullName = BuildDisplayNameFromEmail(email),
                    EmailConfirmed = true,
                    Status = ActiveStatus,
                    SolvoPartnerStatus = ActiveStatus
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return BadRequest(Response<SolvoPartnerDto>.ErrorResponse(ToErrors(createResult), HttpStatusCode.BadRequest));
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!roleResult.Succeeded)
                {
                    return BadRequest(Response<SolvoPartnerDto>.ErrorResponse(ToErrors(roleResult), HttpStatusCode.BadRequest));
                }
            }

            user.SolvoPartnerStatus = ActiveStatus;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(Response<SolvoPartnerDto>.ErrorResponse(ToErrors(updateResult), HttpStatusCode.BadRequest));
            }

            await UpdateSecurityStampForOtherUser(user);

            return Response<SolvoPartnerDto>.SuccessResponse(MapPartner(user), HttpStatusCode.OK);
        }

        [HttpPost("{id}/activate")]
        [Produces<Response<SolvoPartnerDto>>]
        public async Task<ActionResult<Response<SolvoPartnerDto>>> ActivatePartner(string id)
        {
            return await SetPartnerStatus(id, ActiveStatus);
        }

        [HttpPost("{id}/deactivate")]
        [Produces<Response<SolvoPartnerDto>>]
        public async Task<ActionResult<Response<SolvoPartnerDto>>> DeactivatePartner(string id)
        {
            return await SetPartnerStatus(id, InactiveStatus);
        }

        [HttpDelete("{id}")]
        [Produces<Response<bool>>]
        public async Task<ActionResult<Response<bool>>> RemovePartner(string id)
        {
            var user = await FindPartnerUser(id);
            if (user is null)
            {
                return NotFound(Response<bool>.ErrorResponse(["Solvo Partner was not found."], HttpStatusCode.NotFound));
            }

            user.SolvoPartnerStatus = string.Empty;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(Response<bool>.ErrorResponse(ToErrors(result), HttpStatusCode.BadRequest));
            }

            await UpdateSecurityStampForOtherUser(user);

            return Response<bool>.SuccessResponse(true, HttpStatusCode.OK);
        }

        private async Task<ActionResult<Response<SolvoPartnerDto>>> SetPartnerStatus(string id, string status)
        {
            var user = await FindPartnerUser(id);
            if (user is null)
            {
                return NotFound(Response<SolvoPartnerDto>.ErrorResponse(["Solvo Partner was not found."], HttpStatusCode.NotFound));
            }

            user.SolvoPartnerStatus = status;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(Response<SolvoPartnerDto>.ErrorResponse(ToErrors(result), HttpStatusCode.BadRequest));
            }

            await UpdateSecurityStampForOtherUser(user);

            return Response<SolvoPartnerDto>.SuccessResponse(MapPartner(user), HttpStatusCode.OK);
        }

        private async Task<ApplicationUser?> FindPartnerUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null || string.IsNullOrWhiteSpace(user.SolvoPartnerStatus))
            {
                return null;
            }

            return user;
        }

        private async Task UpdateSecurityStampForOtherUser(ApplicationUser user)
        {
            var currentUserId = User.FindFirst("uid")?.Value;
            if (string.Equals(currentUserId, user.Id, StringComparison.Ordinal))
            {
                return;
            }

            await _userManager.UpdateSecurityStampAsync(user);
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

        private static SolvoPartnerDto MapPartner(ApplicationUser user)
        {
            return new SolvoPartnerDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Status = user.SolvoPartnerStatus,
                IsActive = string.Equals(user.SolvoPartnerStatus, ActiveStatus, StringComparison.OrdinalIgnoreCase)
            };
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

    public class CreateSolvoPartnerRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SolvoPartnerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
