using Core.Contracts.Configurations;
using Core.DBContext;
using Core.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Configurations
{
    public class CountryHuntyInformationRepository : ICountryHuntyInformationRepository
    {
        private readonly SolvoRefAppContext _context;
        public CountryHuntyInformationRepository(SolvoRefAppContext context)
        {
            _context = context;
        }
        public async Task<List<CountryHuntyInformation>> GetAll()
        {
            return await _context.CountriesHuntyInformation.AsNoTracking().ToListAsync();
        }
    }
}