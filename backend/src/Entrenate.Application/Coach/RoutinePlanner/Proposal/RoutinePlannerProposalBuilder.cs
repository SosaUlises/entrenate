using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    public static class RoutinePlannerProposalBuilder
    {
        public const string PolicyVersion = "2.5";

        public static RoutinePlannerProposal Build(
            RoutinePlannerInput input)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (!string.Equals(
                input.PolicyVersion,
                PolicyVersion,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"La versión de política debe ser {PolicyVersion}.",
                    nameof(input));
            }

            ArgumentNullException.ThrowIfNull(input.PreferredDays);
            ArgumentNullException.ThrowIfNull(input.AvailableEquipmentIds);
            ArgumentNullException.ThrowIfNull(input.Catalog);

            var preferredDays = input.PreferredDays
                .Distinct()
                .Order()
                .ToArray();
            var availableEquipmentIds = input.AvailableEquipmentIds
                .Distinct()
                .Order()
                .ToArray();
            var catalog = input.Catalog
                .OrderBy(candidate => candidate.Code, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ExerciseId)
                .ToArray();
            var weeklyStructure = WeeklyRoutineStructurePlanner.Build(
                input.DaysPerWeek,
                preferredDays);
            var selection = RoutineExerciseSelector.Select(
                weeklyStructure,
                catalog,
                availableEquipmentIds);
            var prescription = RoutinePrescriptionEngine.Prescribe(
                selection,
                new RoutinePrescriptionProfile(
                    input.Goal,
                    input.ExperienceLevel,
                    input.TargetSessionDurationMinutes));
            var feasibility = MapFeasibility(prescription.Feasibility);
            var days = BuildDays(
                weeklyStructure,
                prescription,
                input.TargetSessionDurationMinutes);
            var canonicalHash = RoutinePlannerProposalCanonicalizer.Calculate(
                input with
                {
                    PreferredDays = preferredDays,
                    AvailableEquipmentIds = availableEquipmentIds,
                    Catalog = catalog
                },
                feasibility,
                days,
                prescription.WeeklyMuscleVolumes,
                selection.Diagnostics,
                prescription.PrescriptionDiagnostics);
            var proposalId = Guid.ParseExact(
                canonicalHash[..32],
                "N");

            return new RoutinePlannerProposal(
                proposalId,
                input.PolicyVersion,
                canonicalHash,
                feasibility,
                input.Goal,
                input.ExperienceLevel,
                input.DaysPerWeek,
                days,
                prescription.WeeklyMuscleVolumes,
                selection.Diagnostics,
                prescription.PrescriptionDiagnostics);
        }

        private static IReadOnlyList<RoutinePlannerProposalDay> BuildDays(
            WeeklyRoutineStructure weeklyStructure,
            WeeklyRoutinePrescription prescription,
            int targetDurationMinutes)
        {
            var prescribedSessions = prescription.Sessions.ToDictionary(
                session => session.DayOrder);

            return weeklyStructure.Days
                .OrderBy(day => day.Order)
                .Select(day =>
                {
                    prescribedSessions.TryGetValue(
                        day.Order,
                        out var session);

                    var exercises = session?.Exercises
                        .OrderBy(exercise => exercise.Order)
                        .Select(exercise =>
                            new RoutinePlannerProposalExercise(
                                exercise.ExerciseId,
                                exercise.Code,
                                exercise.Name,
                                exercise.Order,
                                exercise.MovementPattern,
                                exercise.ExerciseType,
                                exercise.SlotRole,
                                exercise.IsRequired,
                                exercise.Sets,
                                exercise.MinReps,
                                exercise.MaxReps,
                                exercise.MinRir,
                                exercise.MaxRir,
                                exercise.RestSeconds))
                        .ToArray() ?? [];

                    return new RoutinePlannerProposalDay(
                        day.Order,
                        day.ScheduledDay,
                        day.DayOrigin,
                        day.Focus,
                        day.Variant,
                        session?.TargetDurationMinutes ??
                            targetDurationMinutes,
                        session?.EstimatedDurationSeconds ?? 0,
                        exercises);
                })
                .ToArray();
        }

        private static RoutineProposalFeasibility MapFeasibility(
            RoutinePrescriptionFeasibility feasibility)
        {
            return feasibility switch
            {
                RoutinePrescriptionFeasibility.Feasible =>
                    RoutineProposalFeasibility.Feasible,
                RoutinePrescriptionFeasibility.FeasibleWithWarnings =>
                    RoutineProposalFeasibility.FeasibleWithWarnings,
                RoutinePrescriptionFeasibility.NotFeasible =>
                    RoutineProposalFeasibility.NotFeasible,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(feasibility))
            };
        }
    }
}
