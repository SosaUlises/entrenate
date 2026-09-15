using Entrenate.Application.Exercises.DTOs;
using MediatR;

namespace Entrenate.Application.Exercises.Queries.GetExercises
{
    public record GetExercisesQuery
    : IRequest<IReadOnlyCollection<ExerciseDto>>;
}
