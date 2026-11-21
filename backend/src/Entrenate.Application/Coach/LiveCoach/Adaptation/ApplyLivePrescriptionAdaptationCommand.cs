using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed record ApplyLivePrescriptionAdaptationCommand(
        LivePrescriptionAdaptationProposal Proposal)
        : IRequest<TrainingSessionDto>;
}
