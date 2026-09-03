using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralAccount
{
    public class GetAllReferralAccountRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}