using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralFound
{
    public class GetAllReferralFoundRequestHandler : IRequestHandler<GetAllReferralFoundRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralFoundRepository _referralFoundRepository;
        private readonly IMapper _mapper;

        public GetAllReferralFoundRequestHandler(IMapper mapper, IReferralFoundRepository referralFoundRepository)
        {
            _mapper = mapper;
            _referralFoundRepository = referralFoundRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralFoundRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralFoundRepository.GetAllActive();
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}
