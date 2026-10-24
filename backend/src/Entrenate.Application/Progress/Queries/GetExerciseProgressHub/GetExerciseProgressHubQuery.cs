using Entrenate.Application.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgressHub
{
    public record GetExerciseProgressHubQuery
        : IRequest<ExerciseProgressHubDto>;
}
