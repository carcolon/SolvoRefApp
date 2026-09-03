using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.GetActiveVacancies
{
    public class GetActiveVacanciesRequest : IRequest<Response<List<GetActiveVacanciesDto>>>
    {
        
    }
}