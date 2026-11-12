using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed record ApplyLiveTimeConstraintAdaptationCommand(
        LiveTimeConstraintProposal Proposal)
        : IRequest<TrainingSessionDto>;
}
