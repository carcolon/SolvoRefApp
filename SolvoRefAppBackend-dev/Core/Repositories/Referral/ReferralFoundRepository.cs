using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralFoundRepository : IReferralFoundRepository
    {
        private readonly SolvoRefAppContext _context;

        public ReferralFoundRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task<List<ReferralFound>> GetAllActive()
        {
            return await _context.ReferralFounds.AsNoTracking().Where(x => x.Active).ToListAsync();
        }
    }
}