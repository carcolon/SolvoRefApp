using System.Net;
using AutoMapper;
using Core.Contracts.Identity;
using Core.Contracts.Referrals;
using Core.DBContext;
using Core.Feature.Referrals.Common;
using Core.Feature.Referrals.UpdateReferralStatus;
using Core.Models.Global;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Feature.Referrals.GetAllReferrerByUser
{
    public class GetAllReferralByUserRequestHandler : IRequestHandler<GetAllReferralByUserRequest, PagedResponse<List<GetAllReferralByUserDto>>>
    {
        public IReferralRepository _referralRepository;
        public IMapper _mapper;
        public IUserService _userService;
        private readonly IMediator _mediator;
        private readonly SolvoRefAppContext _context;

        public GetAllReferralByUserRequestHandler(IReferralRepository referralRepository, IMapper mapper, IUserService userService, IMediator mediator, SolvoRefAppContext context)
        {
            _referralRepository = referralRepository;
            _mapper = mapper;
            _userService = userService;
            _mediator = mediator;
            _context = context;
        }

        public async Task<PagedResponse<List<GetAllReferralByUserDto>>> Handle(GetAllReferralByUserRequest request, CancellationToken cancellationToken)
        {
            var syncResponse = await _mediator.Send(new UpdateReferralStatusRequest(), cancellationToken);
            var query = _mapper.Map<QueryReferralBase>(request.Query);
            query.UserId = _userService.UserId;
            var queryReferral = _referralRepository.GetQueryReferral(query);
            var totalCount = await queryReferral.CountAsync(cancellationToken: cancellationToken);
            var result = await queryReferral
                .OrderByDescending(x => x.CreationDate)
                .Skip((request.Query.PageNumber - 1) * request.Query.PageSize)
                .Take(request.Query.PageSize)
                .ToListAsync(cancellationToken: cancellationToken);
            var dataResponse = _mapper.Map<List<GetAllReferralByUserDto>>(result);
            if (await IsCurrentUserFromKenya(cancellationToken))
            {
                foreach (var referral in dataResponse)
                {
                    referral.PaymentMessage = string.Empty;
                }
            }

            var response = new PagedResponse<List<GetAllReferralByUserDto>>();
            response.Data = dataResponse;
            response.PageSize = request.Query.PageSize;
            response.TotalPages = (int)Math.Ceiling(totalCount / (decimal)request.Query.PageSize);
            response.PageNumber = request.Query.PageNumber;
            response.Success = true;
            response.Errors = syncResponse.Success ? [] : syncResponse.Errors;
            response.StatusCode = HttpStatusCode.OK;
            return response;
        }

        private async Task<bool> IsCurrentUserFromKenya(CancellationToken cancellationToken)
        {
            var userCountry = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == _userService.UserId)
                .Select(user => user.Country)
                .FirstOrDefaultAsync(cancellationToken);

            return string.Equals(userCountry?.Trim(), "Kenya", StringComparison.OrdinalIgnoreCase);
        }
    }
}
