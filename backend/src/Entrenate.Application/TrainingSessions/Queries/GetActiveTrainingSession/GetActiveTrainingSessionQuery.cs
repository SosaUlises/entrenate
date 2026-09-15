using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetActiveTrainingSession
{
    public record GetActiveTrainingSessionQuery
        : IRequest<TrainingSessionDto>;
}
