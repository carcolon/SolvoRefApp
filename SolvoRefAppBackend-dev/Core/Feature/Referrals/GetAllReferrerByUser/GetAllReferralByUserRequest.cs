using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetAllReferrerByUser
{
    public class GetAllReferralByUserRequest : IRequest<PagedResponse<List<GetAllReferralByUserDto>>>
    {
        public QueryReferralUser Query { get; set; }

        public GetAllReferralByUserRequest(QueryReferralUser query)
        {
            Query = query;
        }
    }
}