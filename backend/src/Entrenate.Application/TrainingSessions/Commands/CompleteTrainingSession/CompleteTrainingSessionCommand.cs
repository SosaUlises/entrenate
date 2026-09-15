using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.CompleteTrainingSession
{
    public record CompleteTrainingSessionCommand(Guid SessionId) : IRequest;
}
