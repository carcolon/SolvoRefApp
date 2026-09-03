using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralEnglishLevelRepository : IReferralEnglishLevelRepository
    {
        private readonly SolvoRefAppContext _context;
        public ReferralEnglishLevelRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task<List<ReferralEnglishLevel>> GetAllActive()
        {
            return await _context.ReferralEnglishLevels.AsNoTracking().Where(x => x.Active).ToListAsync();
        }
    }
}