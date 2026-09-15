using Entrenate.Application.Exercises.DTOs;
using MediatR;

namespace Entrenate.Application.Exercises.Queries.GetExerciseById
{
    public record GetExerciseByIdQuery(Guid Id)
    : IRequest<ExerciseDto>;
}
