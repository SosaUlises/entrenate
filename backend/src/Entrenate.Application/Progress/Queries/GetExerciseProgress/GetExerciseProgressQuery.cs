using Entrenate.Application.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgress
{
    public record GetExerciseProgressQuery(Guid ExerciseId)
        : IRequest<ExerciseProgressDto>;
}
