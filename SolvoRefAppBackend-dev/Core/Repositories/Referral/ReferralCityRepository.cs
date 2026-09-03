using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralCityRepository : IReferralCityRepository
    {
        private readonly SolvoRefAppContext _context;
        public ReferralCityRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task<List<ReferralCity>> GetAllCityByCountryActive(int countryId)
        {
            return await _context.ReferralCities.AsNoTracking().Where(x => x.Active && x.CountryId == countryId).ToListAsync();
        }
    }
}