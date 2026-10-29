using System.Security.Cryptography;
using System.Text;
using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    internal static class RoutinePlannerProposalCanonicalizer
    {
        private const string CanonicalFormatVersion =
            "Entrenate.RoutinePlannerProposal.v2";

        public static string Calculate(
            RoutinePlannerInput input,
            RoutineProposalFeasibility feasibility,
            IReadOnlyCollection<RoutinePlannerProposalDay> days,
            IReadOnlyCollection<WeeklyMuscleVolume> weeklyMuscleVolumes,
            IReadOnlyCollection<ExerciseSelectionDiagnostic>
                selectionDiagnostics,
            IReadOnlyCollection<RoutinePrescriptionDiagnostic>
                prescriptionDiagnostics)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(
                stream,
                Encoding.UTF8,
                leaveOpen: true);

            writer.Write(CanonicalFormatVersion);
            writer.Write(input.PolicyVersion);
            writer.Write((int)input.Goal);
            writer.Write((int)input.ExperienceLevel);
            writer.Write(input.DaysPerWeek);
            writer.Write(input.TargetSessionDurationMinutes);

            WriteIntegers(
                writer,
                input.PreferredDays
                    .Distinct()
                    .Order()
                    .Select(day => (int)day));
            WriteStrings(
                writer,
                input.AvailableEquipmentIds
                    .Distinct()
                    .Order()
                    .Select(id => id.ToString("N")));
            WriteCatalog(writer, input.Catalog);

            writer.Write((int)feasibility);
            WriteDays(writer, days);
            WriteVolumes(writer, weeklyMuscleVolumes);
            WriteSelectionDiagnostics(writer, selectionDiagnostics);
            WritePrescriptionDiagnostics(
                writer,
                prescriptionDiagnostics);
            writer.Flush();

            return Convert.ToHexString(SHA256.HashData(stream.ToArray()))
                .ToLowerInvariant();
        }

        private static void WriteCatalog(
            BinaryWriter writer,
            IEnumerable<PlannerExerciseCandidate> catalog)
        {
            var orderedCatalog = catalog
                .OrderBy(candidate => candidate.Code, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ExerciseId)
                .ToArray();

            writer.Write(orderedCatalog.Length);

            foreach (var candidate in orderedCatalog)
            {
                writer.Write(candidate.Code ?? string.Empty);
                writer.Write(candidate.PrimaryMuscleGroup);
                writer.Write(candidate.MovementPattern.HasValue);
                writer.Write((int?)candidate.MovementPattern ?? 0);
                writer.Write(candidate.ExerciseType.HasValue);
                writer.Write((int?)candidate.ExerciseType ?? 0);
                writer.Write(candidate.IsActive);

                WriteStrings(
                    writer,
                    candidate.SecondaryMuscleGroups
                        .Where(group => !string.IsNullOrWhiteSpace(group))
                        .Select(group => group.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(group => group, StringComparer.Ordinal));

                var equipment = candidate.EquipmentRequirements
                    .OrderBy(requirement => requirement.EquipmentId)
                    .ThenBy(requirement => requirement.IsActive)
                    .ToArray();
                writer.Write(equipment.Length);

                foreach (var requirement in equipment)
                {
                    writer.Write(requirement.EquipmentId.ToString("N"));
                    writer.Write(requirement.IsActive);
                }
            }
        }

        private static void WriteDays(
            BinaryWriter writer,
            IEnumerable<RoutinePlannerProposalDay> days)
        {
            var orderedDays = days.OrderBy(day => day.Order).ToArray();
            writer.Write(orderedDays.Length);

            foreach (var day in orderedDays)
            {
                writer.Write(day.Order);
                writer.Write((int)day.ScheduledDay);
                writer.Write((int)day.DayOrigin);
                writer.Write((int)day.Focus);
                writer.Write((int)day.Variant);
                writer.Write(day.TargetDurationMinutes);
                writer.Write(day.EstimatedDurationSeconds);

                var exercises = day.Exercises
                    .OrderBy(exercise => exercise.Order)
                    .ToArray();
                writer.Write(exercises.Length);

                foreach (var exercise in exercises)
                {
                    writer.Write(exercise.Code);
                    writer.Write(exercise.Order);
                    writer.Write((int)exercise.MovementPattern);
                    writer.Write((int)exercise.ExerciseType);
                    writer.Write((int)exercise.Role);
                    writer.Write(exercise.IsRequired);
                    writer.Write(exercise.Sets);
                    writer.Write(exercise.MinReps);
                    writer.Write(exercise.MaxReps);
                    writer.Write(exercise.MinRir);
                    writer.Write(exercise.MaxRir);
                    writer.Write(exercise.RestSeconds);
                }
            }
        }

        private static void WriteVolumes(
            BinaryWriter writer,
            IEnumerable<WeeklyMuscleVolume> volumes)
        {
            var orderedVolumes = volumes
                .OrderBy(volume => volume.MuscleGroup, StringComparer.Ordinal)
                .ToArray();
            writer.Write(orderedVolumes.Length);

            foreach (var volume in orderedVolumes)
            {
                writer.Write(volume.MuscleGroup);
                writer.Write(volume.EffectiveSets);
            }
        }

        private static void WriteSelectionDiagnostics(
            BinaryWriter writer,
            IEnumerable<ExerciseSelectionDiagnostic> diagnostics)
        {
            var orderedDiagnostics = diagnostics
                .OrderBy(diagnostic => diagnostic.Code)
                .ThenBy(diagnostic => diagnostic.DayOrder)
                .ThenBy(diagnostic => diagnostic.SlotPosition)
                .ToArray();
            writer.Write(orderedDiagnostics.Length);

            foreach (var diagnostic in orderedDiagnostics)
            {
                writer.Write((int)diagnostic.Code);
                writer.Write(diagnostic.DayOrder);
                writer.Write(diagnostic.SlotPosition);
            }
        }

        private static void WritePrescriptionDiagnostics(
            BinaryWriter writer,
            IEnumerable<RoutinePrescriptionDiagnostic> diagnostics)
        {
            var orderedDiagnostics = diagnostics
                .OrderBy(diagnostic => diagnostic.Code)
                .ThenBy(diagnostic => diagnostic.DayOrder)
                .ThenBy(diagnostic => diagnostic.ExerciseCode,
                    StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.MovementPattern)
                .ThenBy(diagnostic => diagnostic.MuscleGroup,
                    StringComparer.Ordinal)
                .ToArray();
            writer.Write(orderedDiagnostics.Length);

            foreach (var diagnostic in orderedDiagnostics)
            {
                writer.Write((int)diagnostic.Code);
                WriteNullable(writer, diagnostic.DayOrder);
                writer.Write(diagnostic.ExerciseCode ?? string.Empty);
                WriteNullable(
                    writer,
                    diagnostic.MovementPattern.HasValue
                        ? (int)diagnostic.MovementPattern.Value
                        : null);
                writer.Write(diagnostic.MuscleGroup ?? string.Empty);
            }
        }

        private static void WriteNullable(
            BinaryWriter writer,
            int? value)
        {
            writer.Write(value.HasValue);

            if (value.HasValue)
            {
                writer.Write(value.Value);
            }
        }

        private static void WriteIntegers(
            BinaryWriter writer,
            IEnumerable<int> values)
        {
            var items = values.ToArray();
            writer.Write(items.Length);

            foreach (var item in items)
            {
                writer.Write(item);
            }
        }

        private static void WriteStrings(
            BinaryWriter writer,
            IEnumerable<string> values)
        {
            var items = values.ToArray();
            writer.Write(items.Length);

            foreach (var item in items)
            {
                writer.Write(item);
            }
        }
    }
}
