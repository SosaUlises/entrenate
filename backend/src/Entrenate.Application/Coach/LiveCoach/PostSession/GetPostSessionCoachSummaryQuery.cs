using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.PostSession
{
    public sealed record GetPostSessionCoachSummaryQuery(Guid SessionId)
        : IRequest<PostSessionCoachSummaryDto>;
}
