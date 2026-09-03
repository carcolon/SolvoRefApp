using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Referral
{
    public class ReferralExperienceRepository : IReferralExperienceRepository
    {
        private readonly SolvoRefAppContext _context;
        public ReferralExperienceRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task<List<ReferralExperience>> GetAllActive()
        {
            return await _context.ReferralExperiences.AsNoTracking().Where(x => x.Active).ToListAsync();
        }
    }
}