using Entrenate.Application.Coach.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.Progress.Queries.GetCoachProgress
{
    public record GetCoachProgressQuery(Guid ExerciseId)
        : IRequest<CoachProgressDto>;
}
