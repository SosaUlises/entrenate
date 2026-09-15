using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById
{
    public record GetTrainingSessionByIdQuery(Guid SessionId)
        : IRequest<TrainingSessionDto>;
}
