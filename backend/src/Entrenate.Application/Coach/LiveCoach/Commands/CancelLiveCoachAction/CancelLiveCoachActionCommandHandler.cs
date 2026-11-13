using Entrenate.Application.Coach.LiveCoach.Actions;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.CancelLiveCoachAction
{
    public sealed class CancelLiveCoachActionCommandHandler
        : IRequestHandler<CancelLiveCoachActionCommand>
    {
        private readonly ILiveCoachPendingActionExecutor _executor;

        public CancelLiveCoachActionCommandHandler(ILiveCoachPendingActionExecutor executor)
        {
            _executor = executor;
        }

        public Task Handle(
            CancelLiveCoachActionCommand request,
            CancellationToken cancellationToken)
            => _executor.CancelAsync(request.ActionId, cancellationToken);
    }
}
