using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Feature.Referrals.Common;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public class ReferralRepository : IReferralRepository
    {
        private readonly SolvoRefAppContext _context;

        public ReferralRepository(SolvoRefAppContext context)
        {
            _context = context;
        }

        public async Task CreateReferral(Models.Referrals.Referral referral)
        {
            await _context.Referral.AddAsync(referral);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsDuplicateSubmission(string submissionKey)
        {
            return _context.Referral.AnyAsync(x => x.ReferralSubmissionKey == submissionKey);
        }

        public Task<bool> ExistsCandidateReferral(string referralId, string email)
        {
            var normalizedReferralId = Normalize(referralId);
            var normalizedEmail = Normalize(email);

            return _context.Referral.AnyAsync(x =>
                x.ReferralID.Trim().ToLower() == normalizedReferralId &&
                x.Email.Trim().ToLower() == normalizedEmail);
        }

        public async Task<bool> ValidateReferralId(string referralId)
        {
            var referral = await _context.Referral.FirstOrDefaultAsync(x => x.ReferralID == referralId);
            return referral != null;
        }

        public IQueryable<Models.Referrals.Referral?> GetQueryReferral(QueryReferralBase query)
        {
            var querySearch = _context.Referral.AsNoTracking();
            if (!string.IsNullOrEmpty(query.Status))
            {
                querySearch = querySearch.Where(x => x.Status.ToLower() == query.Status.ToLower());
            }

            if (!string.IsNullOrEmpty(query.UserId))
            {
                querySearch = querySearch.Where(x => x.ReferrerID == query.UserId);
            }
            return querySearch;
        }

        public Task<List<Models.Referrals.Referral>> GetReferralsOpen()
        {
            return _context.Referral
                .Include(x => x.Referrer)
                .Where(r => r.Updatable || r.Status.ToLower() == "referral expired")
                .ToListAsync();
        }

        public async Task Update(List<Models.Referrals.Referral> data)
        {
            _context.Referral.UpdateRange(data);
            await _context.SaveChangesAsync();
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
