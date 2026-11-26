using Entrenate.Application.Coach.LiveCoach.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed record ApplyLiveAdaptationCommand(
        ILiveAdaptationProposal Proposal)
        : IRequest<LiveCoachActionConfirmationDto>;
}
