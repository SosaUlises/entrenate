using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionHistory
{
    public record GetTrainingSessionHistoryQuery
        : IRequest<IReadOnlyCollection<TrainingSessionHistorySummaryDto>>;
}
