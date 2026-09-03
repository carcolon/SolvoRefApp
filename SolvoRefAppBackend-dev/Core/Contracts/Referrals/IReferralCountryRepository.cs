using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralCountryRepository
    {
        Task<List<ReferralCountry>> GetAllActive();
        Task<ReferralCountry?> GetById(int id);
    }
}