using Core.Models.Referrals;

namespace Core.Contracts.Referrals {
    public interface IReferralVacancyRepository
    {
        Task<List<ReferralVacancy>> GetAllActive();
        Task ReplaceAll(List<ReferralVacancy> vacancies, CancellationToken cancellationToken = default);
    }
}
