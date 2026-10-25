using Entrenate.Application.Coach.RoutinePlanner.Explanations;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;

namespace Entrenate.Application.Coach.RoutinePlanner.Conversation
{
    public static class RoutinePlannerConversationContextBuilder
    {
        public static RoutinePlannerConversationContext Build(
            RoutinePlannerProposal proposal,
            RoutinePlannerExplanation explanation)
        {
            ArgumentNullException.ThrowIfNull(proposal);
            ArgumentNullException.ThrowIfNull(explanation);

            return new RoutinePlannerConversationContext(
                proposal.Goal,
                proposal.ExperienceLevel,
                proposal.RequestedDaysPerWeek,
                BuildFeasibility(proposal),
                proposal.Days
                    .OrderBy(day => day.Order)
                    .Select(MapSession)
                    .ToArray(),
                proposal.WeeklyMuscleVolumes
                    .OrderBy(volume => volume.MuscleGroup,
                        StringComparer.Ordinal)
                    .Select(volume =>
                        new RoutinePlannerConversationMuscleVolume(
                            volume.MuscleGroup,
                            volume.EffectiveSets))
                    .ToArray(),
                explanation.Warnings.ToArray(),
                explanation);
        }

        private static RoutinePlannerConversationFeasibility BuildFeasibility(
            RoutinePlannerProposal proposal)
        {
            return proposal.Feasibility switch
            {
                RoutineProposalFeasibility.Feasible => new(
                    true,
                    false,
                    "La propuesta está completa y puede confirmarse."),
                RoutineProposalFeasibility.FeasibleWithWarnings => new(
                    true,
                    true,
                    "La propuesta puede confirmarse, aunque tiene algunas " +
                        "limitaciones para revisar."),
                RoutineProposalFeasibility.NotFeasible => new(
                    false,
                    true,
                    "La propuesta no puede confirmarse con las " +
                        "restricciones actuales."),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(proposal))
            };
        }

        private static RoutinePlannerConversationSession MapSession(
            RoutinePlannerProposalDay day)
        {
            return new RoutinePlannerConversationSession(
                day.Order,
                day.ScheduledDay,
                day.DayOrigin == ScheduledDayOrigin.Preferred,
                day.Focus,
                day.Variant,
                day.TargetDurationMinutes,
                (int)Math.Ceiling(day.EstimatedDurationSeconds / 60m),
                day.Exercises
                    .OrderBy(exercise => exercise.Order)
                    .Select(exercise =>
                        new RoutinePlannerConversationExercise(
                            exercise.Name,
                            exercise.MovementPattern,
                            exercise.ExerciseType,
                            exercise.Role,
                            exercise.Sets,
                            exercise.MinReps,
                            exercise.MaxReps,
                            exercise.MinRir,
                            exercise.MaxRir,
                            exercise.RestSeconds))
                    .ToArray());
        }
    }
}
