using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction
{
    public sealed record ConfirmLiveCoachActionCommand(Guid ActionId)
        : IRequest<TrainingSessionDto>;
}
