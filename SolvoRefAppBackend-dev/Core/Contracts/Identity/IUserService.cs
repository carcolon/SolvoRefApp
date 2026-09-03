using Core.Models.Identity;

namespace Core.Contracts.Identity
{
    public interface IUserService
    {
        public string UserId { get; }
        public string PrismId { get; }
        public string Name { get; }
        public string Email { get; }
        Task<List<ApplicationUser>> GetUsers();
        Task UpdateUsers(ApplicationUser user);
    }
}