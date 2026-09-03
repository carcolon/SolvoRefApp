
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralCountry
{
    public class GetAllReferralCountryRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}