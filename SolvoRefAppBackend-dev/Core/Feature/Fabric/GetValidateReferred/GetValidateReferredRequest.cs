using Core.Models.Global;
using MediatR;

namespace Core.Feature.Fabric.GetValidateReferred
{
    public class GetValidateReferredRequest : IRequest<Response<GetValidateReferredDto>>
    {
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ReferralId { get; set; } = string.Empty;

        public GetValidateReferredRequest(string phone, string email, string referralId)
        {
            Email = email;
            Phone = phone;
            ReferralId = referralId;
        }
    }
}