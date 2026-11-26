using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Proactive;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Queries.EvaluateLiveCoachProactive
{
    public sealed record EvaluateLiveCoachProactiveQuery(
        Guid SessionId,
        LiveCoachProactiveSnapshot? Snapshot = null)
        : IRequest<LiveCoachProactiveEvaluationDto>;
}
