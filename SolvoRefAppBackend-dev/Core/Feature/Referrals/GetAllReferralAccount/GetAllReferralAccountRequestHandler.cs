using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using Core.Models.Referrals;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralAccount
{
    public class GetAllReferralAccountRequestHandler : IRequestHandler<GetAllReferralAccountRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralAccountRepository _referralAccountRepository;
        private readonly IMapper _mapper;

        public GetAllReferralAccountRequestHandler(IReferralAccountRepository referralAccountRepository, IMapper mapper)
        {
            _referralAccountRepository = referralAccountRepository;
            _mapper = mapper;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralAccountRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralAccountRepository.GetAllActive();
            var returnData = _mapper.Map<List<SelectStructure>>(referralDb);
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}