using Entrenate.Application.Coach.LiveCoach.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction
{
    public sealed record ConfirmLiveCoachActionCommand(Guid ActionId, bool RestTimerIsActive)
        : IRequest<LiveCoachActionConfirmationDto>;
}
