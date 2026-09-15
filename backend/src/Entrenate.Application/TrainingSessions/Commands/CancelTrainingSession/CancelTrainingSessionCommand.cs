using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.CancelTrainingSession
{
    public record CancelTrainingSessionCommand(Guid SessionId) : IRequest;
}
