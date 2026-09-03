

using Core.Contracts.Configurations;
using Core.DBContext;
using Core.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories.Configurations
{
    public class HolyDatesCountryCodeRepository : IHolyDatesCountryCodeRepository
    {
        private readonly SolvoRefAppContext _context;
        public HolyDatesCountryCodeRepository(SolvoRefAppContext context)
        {
            _context = context;
        }
        public async Task<List<HolyDatesCountryCode>?> GetByName(List<string> CountryNames)
        {
            return await _context.HolyDatesCountryCodes.Where(x => CountryNames.Contains(x.DataLakeCountryName.ToLower())).ToListAsync();
        }
    }
}