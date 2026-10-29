using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.AdaptiveTraining.Queries.GenerateSessionAdaptationProposal
{
    public sealed class GenerateSessionAdaptationProposalQueryHandler
        : IRequestHandler<GenerateSessionAdaptationProposalQuery, SessionAdaptationProposalDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly SessionAdaptationProposalGenerator _proposalGenerator;

        public GenerateSessionAdaptationProposalQueryHandler(
            ICurrentUserService currentUserService,
            SessionAdaptationProposalGenerator proposalGenerator)
        {
            _currentUserService = currentUserService;
            _proposalGenerator = proposalGenerator;
        }

        public async Task<SessionAdaptationProposalDto> Handle(
            GenerateSessionAdaptationProposalQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var proposal = await _proposalGenerator.GenerateForUserAsync(
                userId,
                request.RoutineDayId,
                request.AvailableMinutes,
                request.AvailableEquipmentIds,
                request.ExcludedExerciseIds,
                request.EffortPreference,
                cancellationToken);

            return MapProposal(proposal);
        }

        public static SessionAdaptationProposalDto MapProposal(SessionAdaptationProposal proposal)
        {
            var contextDto = new AdaptiveContextDto(
                proposal.Source.RoutineDayId,
                proposal.Context.AvailableMinutes,
                proposal.Context.AvailableEquipmentIds.ToList(),
                proposal.Context.ExcludedExerciseIds.ToList(),
                proposal.Context.EffortPreference.ToString());

            var originalExercisesDto = proposal.OriginalSession.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new AdaptiveExerciseDto(
                    e.ExerciseId,
                    e.ExerciseCode,
                    e.Name,
                    e.Order,
                    e.MovementPattern?.ToString(),
                    e.ExerciseType?.ToString(),
                    e.Priority.ToString(),
                    e.Prescription.Sets,
                    e.Prescription.MinReps,
                    e.Prescription.MaxReps,
                    e.Prescription.MinRir,
                    e.Prescription.MaxRir,
                    e.Prescription.RestSeconds,
                    e.Prescription.Notes))
                .ToList();

            var adaptedExercisesDto = proposal.AdaptedSession.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new AdaptiveExerciseDto(
                    e.ExerciseId,
                    e.ExerciseCode,
                    e.Name,
                    e.Order,
                    e.MovementPattern?.ToString(),
                    e.ExerciseType?.ToString(),
                    e.Priority.ToString(),
                    e.Prescription.Sets,
                    e.Prescription.MinReps,
                    e.Prescription.MaxReps,
                    e.Prescription.MinRir,
                    e.Prescription.MaxRir,
                    e.Prescription.RestSeconds,
                    e.Prescription.Notes))
                .ToList();

            var changesDto = proposal.Changes
                .Select(c => new AdaptiveChangeDto(
                    c.ChangeType.ToString(),
                    c.SourceRoutineExerciseId,
                    c.ExerciseId,
                    c.ExerciseCode,
                    c.SubstitutedExerciseId,
                    c.SubstitutedExerciseCode,
                    c.OriginalSets,
                    c.AdaptedSets,
                    c.OriginalMinRir,
                    c.OriginalMaxRir,
                    c.AdaptedMinRir,
                    c.AdaptedMaxRir,
                    c.SubstitutionReason?.ToString()))
                .ToList();

            var diagnosticsDto = proposal.Diagnostics
                .Select(d => new AdaptiveDiagnosticDto(
                    d.Code.ToString(),
                    "Warning",
                    $"Diagnóstico de adaptación: {d.Code}"))
                .ToList();

            return new SessionAdaptationProposalDto(
                proposal.AdaptationProposalId.ToString(),
                proposal.ProposalHash,
                proposal.PolicyVersion,
                proposal.Feasibility.ToString(),
                contextDto,
                proposal.OriginalSession.EstimatedDurationSeconds,
                proposal.AdaptedSession.EstimatedDurationSeconds,
                originalExercisesDto,
                adaptedExercisesDto,
                changesDto,
                diagnosticsDto);
        }
    }
}
