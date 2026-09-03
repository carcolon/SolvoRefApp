using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralEnglishLevel
{
    public class GetAllReferralEnglishLevelRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}