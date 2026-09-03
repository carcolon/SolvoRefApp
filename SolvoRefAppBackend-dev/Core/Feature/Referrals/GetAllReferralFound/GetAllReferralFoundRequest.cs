using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralFound
{
    public class GetAllReferralFoundRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}