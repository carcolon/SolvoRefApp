using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralCountryRepository : IReferralCountryRepository
    {
        private readonly SolvoRefAppContext _context;
        public ReferralCountryRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task<List<ReferralCountry>> GetAllActive()
        {
            return await _context.ReferralCountries.AsNoTracking().Where(x => x.Active).ToListAsync();
        }

        public async Task<ReferralCountry?> GetById(int id)
        {
            return await _context.ReferralCountries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}