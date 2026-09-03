using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralEnglishLevel
{
    public class GetAllReferralEnglishLevelRequestHandler : IRequestHandler<GetAllReferralEnglishLevelRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralEnglishLevelRepository _referralEnglishLevelRepository;
        private readonly IMapper _mapper;

        public GetAllReferralEnglishLevelRequestHandler(IMapper mapper, IReferralEnglishLevelRepository referralEnglishLevelRepository)
        {
            _mapper = mapper;
            _referralEnglishLevelRepository = referralEnglishLevelRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralEnglishLevelRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralEnglishLevelRepository.GetAllActive();
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}