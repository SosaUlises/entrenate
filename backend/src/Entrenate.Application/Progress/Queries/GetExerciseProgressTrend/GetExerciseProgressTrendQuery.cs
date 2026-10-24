using Entrenate.Application.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgressTrend
{
    public record GetExerciseProgressTrendQuery(
        Guid ExerciseId,
        DateOnly? From,
        DateOnly? To,
        string? TimeZone)
        : IRequest<ExerciseProgressTrendDto>;
}
