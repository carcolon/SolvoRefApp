using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralApplyAreaRepository
    {
        Task<List<ReferralApplyArea>> GetAllActive();
    }
}