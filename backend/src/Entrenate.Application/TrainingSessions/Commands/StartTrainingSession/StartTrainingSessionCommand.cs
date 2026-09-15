using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.StartTrainingSession
{
    public record StartTrainingSessionCommand(Guid DiaRutinaId)
        : IRequest<TrainingSessionDto>;
}
