using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralCityByCountry
{
    public class GetAllReferralCityByCountryRequestHandler : IRequestHandler<GetAllReferralCityByCountryRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralCityRepository _referralCityRepository;
        private readonly IMapper _mapper;
        private readonly IReferralCountryRepository _referralCountryRepository;

        public GetAllReferralCityByCountryRequestHandler(IMapper mapper, IReferralCityRepository referralCityRepository, IReferralCountryRepository referralCountryRepository)
        {
            _mapper = mapper;
            _referralCityRepository = referralCityRepository;
            _referralCountryRepository = referralCountryRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralCityByCountryRequest request, CancellationToken cancellationToken)
        {
            var referralCountryList = await _referralCountryRepository.GetAllActive();
            var referralCountryCode = referralCountryList.FirstOrDefault(x => x.Description == request.CountryId);
            var referralCountry = await _referralCountryRepository.GetById(referralCountryCode.Id);
            if (referralCountry == null)
            {
                Response<List<SelectStructure>>.ErrorResponse([$"Country with id {request.CountryId} was not found"], HttpStatusCode.NotFound);
            }
            var referralDb = await _referralCityRepository.GetAllCityByCountryActive(referralCountry.Id);
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}