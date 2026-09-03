using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralAccountRepository
    {
        Task<List<ReferralAccount>> GetAllActive();
    }
}