using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralCityRepository
    {
        Task<List<ReferralCity>> GetAllCityByCountryActive(int countryId);
    }
}