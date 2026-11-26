using Entrenate.Application.Coach.LiveCoach.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed record ApplyLivePrescriptionAdaptationCommand(
        LivePrescriptionAdaptationProposal Proposal)
        : IRequest<LiveCoachActionConfirmationDto>;
}
