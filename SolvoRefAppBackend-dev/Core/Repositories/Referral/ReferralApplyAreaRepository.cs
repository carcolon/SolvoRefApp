using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralApplyAreaRepository : IReferralApplyAreaRepository
    {
        private readonly SolvoRefAppContext _context;

        public ReferralApplyAreaRepository(SolvoRefAppContext context)
        {
            _context = context;
        }
        public async Task<List<ReferralApplyArea>> GetAllActive()
        {
            return await _context.ReferralApplyAreas.AsNoTracking().Where(x => x.Active).ToListAsync();
        }
    }
}