using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Models.Referrals;
using Microsoft.EntityFrameworkCore;

public class ReferralVacancyRepository : IReferralVacancyRepository
{
    private readonly SolvoRefAppContext _context;

    public ReferralVacancyRepository (SolvoRefAppContext context)
    {
        _context = context;
    }

    public async Task<List<ReferralVacancy>> GetAllActive()
    {
        return await _context.Vacancies.AsNoTracking().Where(x => x.Active).ToListAsync();
    }

    public async Task ReplaceAll(List<ReferralVacancy> vacancies, CancellationToken cancellationToken = default)
    {
        await _context.Vacancies.ExecuteDeleteAsync(cancellationToken);
        await _context.Vacancies.AddRangeAsync(vacancies, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
