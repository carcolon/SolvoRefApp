using System.Net;
using AutoMapper;
using Core.Contracts.Identity;
using Core.Contracts.Referrals;
using Core.Feature.Referrals.Common;
using Core.Models.Global;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Feature.Referrals.GetAllReferralStatus
{
    public class GetAllReferralStatusRequestHandler : IRequestHandler<GetAllReferralStatusRequest, Response<GetAllReferralStatusDto>>
    {
        public IReferralRepository _referralRepository;
        public IUserService _userService;

        public GetAllReferralStatusRequestHandler(IUserService userService, IReferralRepository referralRepository)
        {
            _userService = userService;
            _referralRepository = referralRepository;
        }

        public async Task<Response<GetAllReferralStatusDto>> Handle(GetAllReferralStatusRequest request, CancellationToken cancellationToken)
        {
            var data = new GetAllReferralStatusDto();
            var query = new QueryReferralBase();
            query.UserId = _userService.UserId;
            var queryReferral = _referralRepository.GetQueryReferral(query);
            var result = await queryReferral.Select(x => x.Status).Distinct().ToListAsync(cancellationToken: cancellationToken);
            foreach (var item in result)
            {
                var count = await queryReferral.Where(x => x.Status == item).CountAsync();
                data.Statuses.Add(item, count);
            }

            return Response<GetAllReferralStatusDto>.SuccessResponse(data, HttpStatusCode.OK);
        }
    }
}