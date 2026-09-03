using Core.Feature.Identity.UpdateUsersExtraData;
using Core.Models.Global;
using MediatR;

namespace Core.BackgroundTask.UpdateUserExtraDataTask
{
    public class UpdateUserExtraDataTaskScheduler : IRequest<Response<Unit>>
    {
        private readonly IMediator _mediator;

        public UpdateUserExtraDataTaskScheduler(IMediator mediator)
        {
            _mediator = mediator;
        }
        public async Task ScheduleUpdateUserExtraDataTasks()
        {
            await _mediator.Send(new UpdateUsersExtraDataRequest());
        }
    }
}