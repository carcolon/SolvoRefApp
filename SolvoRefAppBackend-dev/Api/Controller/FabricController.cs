using Core.Feature.Fabric.GetValidateReferred;
using Core.Models.Global;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Api.Models;

namespace Api.Controller
{
    [ApiController]
    [Route("api/fabric")]
    public class FabricController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FabricController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "User")]
        [HttpPost("validate/referred")]
        [EnableRateLimiting("fabric-validate")]
        [Produces<Response<GetValidateReferredDto>>]
        public async Task<ActionResult<Response<GetValidateReferredDto>>> GetValidatedReferred([FromBody] ValidateReferredRequestDto request)
        {
            return await _mediator.Send(new GetValidateReferredRequest(request.Phone, request.Email, request.ReferralId));
        }
    }
}
