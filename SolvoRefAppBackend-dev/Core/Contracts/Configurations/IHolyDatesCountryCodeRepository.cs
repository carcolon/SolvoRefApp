using Core.Models.Configurations;

namespace Core.Contracts.Configurations
{
    public interface IHolyDatesCountryCodeRepository
    {
        Task<List<HolyDatesCountryCode>?> GetByName(List<string> CountryNames);
    }
}