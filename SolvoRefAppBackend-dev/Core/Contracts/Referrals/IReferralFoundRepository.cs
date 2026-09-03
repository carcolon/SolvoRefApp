using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralFoundRepository
    {
        Task<List<ReferralFound>> GetAllActive();
    }
}