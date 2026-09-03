using System.Net;
using AutoMapper;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferralCountryPhoneCode
{
    public class GetAllReferralCountryPhoneCodeRequestHandler : IRequestHandler<GetAllReferralCountryPhoneCodeRequest, Response<List<SelectStructure>>>
    {
        private readonly IReferralCountryRepository _referralCountryRepository;

        public GetAllReferralCountryPhoneCodeRequestHandler(IReferralCountryRepository referralCountryRepository)
        {
            _referralCountryRepository = referralCountryRepository;
        }

        public async Task<Response<List<SelectStructure>>> Handle(GetAllReferralCountryPhoneCodeRequest request, CancellationToken cancellationToken)
        {
            var referralDb = await _referralCountryRepository.GetAllActive();
            var returnData = referralDb.Select(x => new SelectStructure()
            {
                Text = $"{x.PhoneCode} {x.Description}",
                Value = x.PhoneCode
            }).ToList();
            return Response<List<SelectStructure>>.SuccessResponse(returnData, HttpStatusCode.OK);
        }
    }
}