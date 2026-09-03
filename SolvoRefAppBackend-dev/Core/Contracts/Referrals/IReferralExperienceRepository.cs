using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralExperienceRepository
    {
        Task<List<ReferralExperience>> GetAllActive();
    }
}