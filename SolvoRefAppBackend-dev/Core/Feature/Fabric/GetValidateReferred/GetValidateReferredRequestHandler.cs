
using System.Net;
using System.Text.RegularExpressions;
using Core.Contracts.Fabric;
using Core.Contracts.Referrals;
using Core.Models.Global;
using MediatR;

namespace Core.Feature.Fabric.GetValidateReferred
{
    public class GetValidateReferredRequestHandler : IRequestHandler<GetValidateReferredRequest, Response<GetValidateReferredDto>>
    {
        private static readonly Regex ReferralIdFormatRegex = new("^[A-Za-z0-9]{5,20}$", RegexOptions.Compiled);
        private readonly IFabricService _fabricService;
        private readonly IReferralRepository _referralRepository;

        public GetValidateReferredRequestHandler(IFabricService fabricService, IReferralRepository referralRepository)
        {
            _fabricService = fabricService;
            _referralRepository = referralRepository;
        }

        public async Task<Response<GetValidateReferredDto>> Handle(GetValidateReferredRequest request, CancellationToken cancellationToken)
        {
            var data = new GetValidateReferredDto();
            var referralId = request.ReferralId?.Trim() ?? string.Empty;
            if (!ReferralIdFormatRegex.IsMatch(referralId))
            {
                data.Message = "ReferralId not recognized.";
                return Response<GetValidateReferredDto>.ErrorResponse(["ReferralId not recognized."], HttpStatusCode.BadRequest);
            }

            var duplicateCandidate = await _referralRepository.ExistsCandidateReferral(referralId, request.Email);
            if (duplicateCandidate)
            {
                data.Message = "This candidate has already been referred.";
                return Response<GetValidateReferredDto>.ErrorResponse(["This candidate has already been referred."], HttpStatusCode.Conflict);
            }

            var validation = await _fabricService.ReferredValidation(request.Phone, request.Email);
            if (!validation.Success)
            {
                return Response<GetValidateReferredDto>.ErrorResponse(validation.Errors, validation.StatusCode);
            }

            if (validation.Data)
            {
                data.Validation = validation.Data;
                data.Message = "Referral can be processed. Requirements met.";
            }
            else
            {
                data.Message = "At this time, the candidate does not meet the program requirements and cannot continue in the process.";
            }
            return Response<GetValidateReferredDto>.SuccessResponse(data, HttpStatusCode.OK);
        }
    }
}
