using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralCountryPhoneCode
{
    public class GetAllReferralCountryPhoneCodeRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}