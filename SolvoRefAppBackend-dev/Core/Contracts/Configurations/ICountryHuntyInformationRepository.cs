using Core.Models.Configurations;

namespace Core.Contracts.Configurations
{
    public interface ICountryHuntyInformationRepository
    {
        Task<List<CountryHuntyInformation>> GetAll();
    }
}