using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetTrainingActivity
{
    public record GetTrainingActivityQuery(
        DateOnly? From,
        DateOnly? To,
        string? TimeZone)
        : IRequest<TrainingActivityDto>;
}
