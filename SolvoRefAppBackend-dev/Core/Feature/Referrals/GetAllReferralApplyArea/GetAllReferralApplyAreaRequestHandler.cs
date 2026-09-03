using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralApplyArea
{
    public class GetAllReferralApplyAreaRequestHandler : IRequestHandler<GetAllReferralApplyAreaRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralApplyAreaRepository _referralApplyAreaRepository;
        private readonly IMapper _mapper;

        public GetAllReferralApplyAreaRequestHandler(IMapper mapper, IReferralApplyAreaRepository referralApplyAreaRepository)
        {
            _mapper = mapper;
            _referralApplyAreaRepository = referralApplyAreaRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralApplyAreaRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralApplyAreaRepository.GetAllActive();
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}
