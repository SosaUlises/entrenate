using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.CancelLiveCoachAction
{
    public sealed record CancelLiveCoachActionCommand(Guid ActionId) : IRequest;
}
