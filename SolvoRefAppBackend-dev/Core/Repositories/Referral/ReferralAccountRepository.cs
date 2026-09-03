using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralAccountRepository : IReferralAccountRepository
    {
        private readonly SolvoRefAppContext _context;

        public ReferralAccountRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task<List<ReferralAccount>> GetAllActive()
        {
            return await _context.ReferralAccounts.AsNoTracking().Where(x => x.Active).ToListAsync();
        }
    }
}