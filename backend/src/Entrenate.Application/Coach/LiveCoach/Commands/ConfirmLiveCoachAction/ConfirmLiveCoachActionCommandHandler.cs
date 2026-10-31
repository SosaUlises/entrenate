using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction
{
    public sealed class ConfirmLiveCoachActionCommandHandler
        : IRequestHandler<ConfirmLiveCoachActionCommand, TrainingSessionDto>
    {
        private readonly ILiveCoachPendingActionExecutor _executor;

        public ConfirmLiveCoachActionCommandHandler(ILiveCoachPendingActionExecutor executor)
        {
            _executor = executor;
        }

        public Task<TrainingSessionDto> Handle(
            ConfirmLiveCoachActionCommand request,
            CancellationToken cancellationToken)
            => _executor.ConfirmAsync(request.ActionId, cancellationToken);
    }
}
