using Core.Feature.Login.LoginUser;
using Core.Models.Identity;

namespace Core.Contracts.Identity
{
    public interface IAuthService
    {
        Task<LoginUserResponseDto> Login(LoginUserRequestDto request, ApplicationUser user);
        Task<string> GenerateToken(ApplicationUser user);
        Task<ApplicationUser?> GetUserByEmail(string email);
        Task<ApplicationUser?> GetUserByEmployeeIdOrSolId(string employeeId);
        Task DeleteUser(ApplicationUser user);
        Task<LoginUserResponseDto> Register(RegisterDtLoginRequest request);
        Task<bool> ValidateToken(string token);
    }
}
