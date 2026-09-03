using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralExperience
{
    public class GetAllReferralExperienceRequestHandler : IRequestHandler<GetAllReferralExperienceRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralExperienceRepository _referralExperienceRepository;
        private readonly IMapper _mapper;

        public GetAllReferralExperienceRequestHandler(IMapper mapper, IReferralExperienceRepository referralExperienceRepository)
        {
            _mapper = mapper;
            _referralExperienceRepository = referralExperienceRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralExperienceRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralExperienceRepository.GetAllActive();
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}