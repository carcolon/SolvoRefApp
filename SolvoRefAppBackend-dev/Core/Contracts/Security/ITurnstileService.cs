using Core.Models.Global;

namespace Core.Contracts.Security
{
    public interface ITurnstileService
    {
        Task<Response<bool>> ValidateToken(string token, string? remoteIp, CancellationToken cancellationToken);
    }
}
