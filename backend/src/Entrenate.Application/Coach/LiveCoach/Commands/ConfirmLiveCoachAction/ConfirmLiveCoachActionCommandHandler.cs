using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction
{
    public sealed class ConfirmLiveCoachActionCommandHandler
        : IRequestHandler<ConfirmLiveCoachActionCommand, LiveCoachActionConfirmationDto>
    {
        private readonly ILiveCoachPendingActionExecutor _executor;

        public ConfirmLiveCoachActionCommandHandler(ILiveCoachPendingActionExecutor executor)
        {
            _executor = executor;
        }

        public Task<LiveCoachActionConfirmationDto> Handle(
            ConfirmLiveCoachActionCommand request,
            CancellationToken cancellationToken)
            => _executor.ConfirmAsync(
                request.ActionId,
                request.RestTimerIsActive,
                cancellationToken);
    }
}
