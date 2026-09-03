
using Core.Feature.Referrals.UpdateReferralStatus;
using MediatR;

namespace Core.BackgroundTask.UpdateReferralStatusTask
{
    public class UpdateReferralStatusTaskScheduler
    {
        private readonly IMediator _mediator;

        public UpdateReferralStatusTaskScheduler(IMediator mediator)
        {
            _mediator = mediator;
        }
        public async Task ScheduleUpdateReferralStatusTasks()
        {
            await _mediator.Send(new UpdateReferralStatusRequest());
        }
    }
}