using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public sealed record AnswerPostSessionCoachQuery(Guid SessionId, string Question)
        : IRequest<PostSessionCoachAnswerDto>;
}
