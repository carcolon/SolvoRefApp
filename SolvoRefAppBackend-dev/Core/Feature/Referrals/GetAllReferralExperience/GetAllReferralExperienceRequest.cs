using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralExperience
{
    public class GetAllReferralExperienceRequest : IRequest<Response<List<SelectStructure>>>
    {

    }
}