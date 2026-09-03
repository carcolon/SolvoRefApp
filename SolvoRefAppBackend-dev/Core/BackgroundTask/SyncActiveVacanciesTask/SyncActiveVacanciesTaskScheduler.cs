using Core.Feature.Referrals.SyncActiveVacancies;
using MediatR;

namespace Core.BackgroundTask.SyncActiveVacanciesTask
{
    public class SyncActiveVacanciesTaskScheduler
    {
        private readonly IMediator _mediator;

        public SyncActiveVacanciesTaskScheduler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task ScheduleSyncActiveVacanciesTasks()
        {
            await _mediator.Send(new SyncActiveVacanciesRequest());
        }
    }
}
