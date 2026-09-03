using System.Security.Claims;
using Core.Contracts.Identity;
using Core.DBContext;
using Core.Models.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Service.Identity
{
    public class UserService : IUserService
    {
        private readonly SolvoRefAppContext _context;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(IHttpContextAccessor contextAccessor, UserManager<ApplicationUser> userManager, SolvoRefAppContext context)
        {

            _contextAccessor = contextAccessor;
            _userManager = userManager;
            _context = context;
        }
        public string UserId { get => _contextAccessor.HttpContext?.User?.FindFirstValue("uid") ?? ""; }

        public string PrismId { get => _contextAccessor.HttpContext?.User?.FindFirstValue("prismId") ?? ""; }

        public string Name { get => _contextAccessor.HttpContext?.User?.FindFirstValue("Name") ?? ""; }

        public string Email { get => _contextAccessor.HttpContext?.User?.FindFirstValue("emailuser") ?? ""; }

        public async Task<List<ApplicationUser>> GetUsers()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task UpdateUsers(ApplicationUser user)
        {

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();
            return;
        }
    }
}