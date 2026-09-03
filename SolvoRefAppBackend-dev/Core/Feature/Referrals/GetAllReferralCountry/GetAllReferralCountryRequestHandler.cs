using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralCountry
{
    public class GetAllReferralCountryRequestHandler : IRequestHandler<GetAllReferralCountryRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralCountryRepository _referralCountryRepository;
        private readonly IMapper _mapper;

        public GetAllReferralCountryRequestHandler(IMapper mapper, IReferralCountryRepository referralCountryRepository)
        {
            _mapper = mapper;
            _referralCountryRepository = referralCountryRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralCountryRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralCountryRepository.GetAllActive();
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}