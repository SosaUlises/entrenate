using Entrenate.Application.Coach.RoutinePlanner.DTOs;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Queries.GenerateRoutineProposal
{
    public sealed class GenerateRoutineProposalQueryHandler
        : IRequestHandler<
            GenerateRoutineProposalQuery,
            RoutinePlannerProposalDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly RoutinePlannerProposalGenerator _proposalGenerator;

        public GenerateRoutineProposalQueryHandler(
            ICurrentUserService currentUserService,
            RoutinePlannerProposalGenerator proposalGenerator)
        {
            _currentUserService = currentUserService;
            _proposalGenerator = proposalGenerator;
        }

        public async Task<RoutinePlannerProposalDto> Handle(
            GenerateRoutineProposalQuery request,
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
                cancellationToken);

            return MapProposal(proposal);
        }

        private static RoutinePlannerProposalDto MapProposal(
            RoutinePlannerProposal proposal)
        {
            return new RoutinePlannerProposalDto(
                proposal.ProposalId,
                proposal.PolicyVersion,
                proposal.CanonicalHash,
                proposal.Feasibility.ToString(),
                proposal.Goal.ToString(),
                proposal.ExperienceLevel.ToString(),
                proposal.RequestedDaysPerWeek,
                proposal.Days
                    .Select(day => new RoutinePlannerProposalDayDto(
                        day.Order,
                        day.ScheduledDay.ToString(),
                        day.DayOrigin.ToString(),
                        day.Focus.ToString(),
                        day.Variant.ToString(),
                        day.TargetDurationMinutes,
                        day.EstimatedDurationSeconds,
                        day.Exercises
                            .Select(exercise =>
                                new RoutinePlannerProposalExerciseDto(
                                    exercise.ExerciseId,
                                    exercise.Code,
                                    exercise.Name,
                                    exercise.Order,
                                    exercise.MovementPattern.ToString(),
                                    exercise.ExerciseType.ToString(),
                                    exercise.Role.ToString(),
                                    exercise.Sets,
                                    exercise.MinReps,
                                    exercise.MaxReps,
                                    exercise.MinRir,
                                    exercise.MaxRir,
                                    exercise.RestSeconds))
                            .ToArray()))
                    .ToArray(),
                proposal.WeeklyMuscleVolumes
                    .Select(volume =>
                        new RoutinePlannerWeeklyVolumeDto(
                            volume.MuscleGroup,
                            volume.EffectiveSets))
                    .ToArray(),
                proposal.SelectionDiagnostics
                    .Select(diagnostic =>
                        new RoutinePlannerSelectionDiagnosticDto(
                            diagnostic.Code.ToString(),
                            diagnostic.DayOrder,
                            diagnostic.SlotPosition))
                    .ToArray(),
                proposal.PrescriptionDiagnostics
                    .Select(diagnostic =>
                        new RoutinePlannerPrescriptionDiagnosticDto(
                            diagnostic.Code.ToString(),
                            diagnostic.DayOrder,
                            diagnostic.ExerciseCode,
                            diagnostic.MovementPattern?.ToString(),
                            diagnostic.MuscleGroup))
                    .ToArray());
        }
    }
}
