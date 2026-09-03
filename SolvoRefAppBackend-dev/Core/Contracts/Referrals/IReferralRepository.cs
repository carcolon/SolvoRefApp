using Core.Feature.Referrals.Common;
using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralRepository
    {
        Task CreateReferral(Referral referral);
        Task<bool> ExistsCandidateReferral(string referralId, string email);
        Task<bool> ExistsDuplicateSubmission(string submissionKey);
        IQueryable<Referral?> GetQueryReferral(QueryReferralBase query);
        Task<bool> ValidateReferralId(string referralId);
        Task<List<Referral>> GetReferralsOpen();
        Task Update(List<Referral> data);
    }
}
