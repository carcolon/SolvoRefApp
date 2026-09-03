using Core.Models.Global;
using MediatR;

namespace Core.Feature.Login.LoginUser
{
    public class LoginUserRequest : IRequest<string>
    {
        public string Code { get; set; }
        public string? RedirectUri { get; set; }

        public LoginUserRequest(string code, string? redirectUri = null)
        {
            Code = code;
            RedirectUri = redirectUri;
        }
    }
}
