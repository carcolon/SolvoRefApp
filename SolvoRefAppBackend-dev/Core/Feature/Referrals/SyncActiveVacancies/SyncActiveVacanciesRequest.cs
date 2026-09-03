using Core.Models.Global;
using MediatR;

namespace Core.Feature.Referrals.SyncActiveVacancies
{
    public class SyncActiveVacanciesRequest : IRequest<Response<Unit>>
    {
    }
}
