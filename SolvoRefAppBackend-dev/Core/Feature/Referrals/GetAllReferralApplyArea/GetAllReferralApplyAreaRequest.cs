using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralApplyArea
{
    public class GetAllReferralApplyAreaRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}