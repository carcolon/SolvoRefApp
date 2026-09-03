using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.CreateReferral
{
    public class CreateReferralRequest : IRequest<Response<string>>
    {
        public CreateReferralDto Data { get; set; }
        public string? ReferrerId { get; set; }

        public CreateReferralRequest(CreateReferralDto data, string? referrerId = null)
        {
            Data = data;
            ReferrerId = referrerId;
        }
    }
}
