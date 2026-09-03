using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralCityByCountry
{
    public class GetAllReferralCityByCountryRequest : IRequest<Response<List<SelectStructure>>>
    {
        public string CountryId { get; set; }

        public GetAllReferralCityByCountryRequest(string countryId)
        {
            CountryId = countryId;
        }
    }
}