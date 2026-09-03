using MediatR;
using Core.Contracts.Identity;
using Core.Models.Global;
using System.Net;

namespace Core.Feature.Login.ValidateToken
{
    public class ValidateTokenRequestHandler : IRequestHandler<ValidateTokenRequest, Response<bool>>
    {
        private readonly IAuthService _authService;

        public ValidateTokenRequestHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<Response<bool>> Handle(ValidateTokenRequest request, CancellationToken cancellationToken)
        {
            var validation = await _authService.ValidateToken(request.Token);
            return Response<bool>.SuccessResponse(validation, HttpStatusCode.OK);
        }
    }
}