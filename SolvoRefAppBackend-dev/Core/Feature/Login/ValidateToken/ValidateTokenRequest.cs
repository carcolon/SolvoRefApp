using MediatR;
using Core.Models.Global;

namespace Core.Feature.Login.ValidateToken
{
    public class ValidateTokenRequest : IRequest<Response<bool>>
    {
        public string Token { get; set; }

        public ValidateTokenRequest(string token)
        {
            Token = token;
        }
    }
}