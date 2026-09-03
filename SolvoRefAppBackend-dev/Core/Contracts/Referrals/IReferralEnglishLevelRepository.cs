using Core.Models.Referrals;

namespace Core.Contracts.Referrals
{
    public interface IReferralEnglishLevelRepository
    {
        Task<List<ReferralEnglishLevel>> GetAllActive();
    }
}