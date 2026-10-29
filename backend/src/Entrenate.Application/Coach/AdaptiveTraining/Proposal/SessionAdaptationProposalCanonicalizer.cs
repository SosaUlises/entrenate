using System.Security.Cryptography;
using System.Text;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;

namespace Entrenate.Application.Coach.AdaptiveTraining.Proposal
{
    internal static class SessionAdaptationProposalCanonicalizer
    {
        private const string CanonicalFormatVersion =
            "Entrenate.SessionAdaptationProposal.v3";

        public static (string Hash, Guid ProposalId) Calculate(
            string policyVersion,
            PlannedAdaptiveSession originalSession,
            SessionAdaptationRequest request,
            IReadOnlyCollection<AdaptiveSubstitutionCandidate>? catalog,
            SessionAdaptationFeasibility feasibility,
            SessionPlanSnapshot adaptedSession,
            IReadOnlyList<SessionAdaptationChange> changes,
            IReadOnlyList<SessionAdaptationDiagnostic> diagnostics)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(
                stream,
                Encoding.UTF8,
                leaveOpen: true);

            writer.Write(CanonicalFormatVersion);
            writer.Write(policyVersion);
            writer.Write(originalSession.RoutineDayId.ToString("N"));
            writer.Write(originalSession.RoutineId.HasValue);
            writer.Write(originalSession.RoutineId?.ToString("N") ?? string.Empty);

            // Context
            writer.Write(request.AvailableMinutes.HasValue);
            writer.Write(request.AvailableMinutes ?? 0);
            writer.Write((int)request.EffortPreference);

            WriteSortedGuids(writer, request.AvailableEquipmentIds);
            WriteSortedGuids(writer, request.ExcludedExerciseIds);

            // Original exercises
            WriteOriginalExercises(writer, originalSession.Exercises);

            // Relevant catalog
            WriteCatalog(writer, catalog);

            // Feasibility
            writer.Write((int)feasibility);

            // Adapted session
            WriteAdaptedSession(writer, adaptedSession);

            // Changes
            WriteChanges(writer, changes);

            // Diagnostics
            WriteDiagnostics(writer, diagnostics);

            writer.Flush();

            var hashBytes = SHA256.HashData(stream.ToArray());
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            var proposalId = Guid.ParseExact(hash[..32], "N");

            return (hash, proposalId);
        }

        private static void WriteSortedGuids(
            BinaryWriter writer,
            IEnumerable<Guid>? guids)
        {
            var sorted = (guids ?? [])
                .Where(id => id != Guid.Empty)
                .Distinct()
                .OrderBy(id => id)
                .Select(id => id.ToString("N"))
                .ToArray();

            writer.Write(sorted.Length);
            foreach (var guid in sorted)
            {
                writer.Write(guid);
            }
        }

        private static void WriteOriginalExercises(
            BinaryWriter writer,
            IReadOnlyCollection<PlannedAdaptiveExercise> exercises)
        {
            var ordered = exercises
                .OrderBy(e => e.Order)
                .ThenBy(e => e.ExerciseCode, StringComparer.Ordinal)
                .ThenBy(e => e.ExerciseId)
                .ToArray();

            writer.Write(ordered.Length);

            foreach (var exercise in ordered)
            {
                writer.Write(exercise.SourceRoutineExerciseId.ToString("N"));
                writer.Write(exercise.ExerciseId.ToString("N"));
                writer.Write(exercise.ExerciseCode ?? string.Empty);
                writer.Write(exercise.Order);
                writer.Write(exercise.MovementPattern.HasValue);
                writer.Write((int?)exercise.MovementPattern ?? 0);
                writer.Write(exercise.ExerciseType.HasValue);
                writer.Write((int?)exercise.ExerciseType ?? 0);
                writer.Write(exercise.PrimaryMuscleGroup?.Trim().ToLowerInvariant() ?? string.Empty);
                writer.Write(exercise.IsRequiredForAdaptation.HasValue);
                writer.Write(exercise.IsRequiredForAdaptation ?? false);

                WriteSortedStrings(writer, exercise.SecondaryMuscleGroups);
                WriteSortedGuids(writer, exercise.RequiredEquipmentIds);

                writer.Write(exercise.Prescription.Sets);
                writer.Write(exercise.Prescription.MinReps);
                writer.Write(exercise.Prescription.MaxReps);
                writer.Write(exercise.Prescription.MinRir);
                writer.Write(exercise.Prescription.MaxRir);
                writer.Write(exercise.Prescription.RestSeconds);
            }
        }

        private static void WriteCatalog(
            BinaryWriter writer,
            IEnumerable<AdaptiveSubstitutionCandidate>? catalog)
        {
            var ordered = (catalog ?? [])
                .OrderBy(c => c.Code, StringComparer.Ordinal)
                .ThenBy(c => c.ExerciseId)
                .ToArray();

            writer.Write(ordered.Length);

            foreach (var candidate in ordered)
            {
                writer.Write(candidate.ExerciseId.ToString("N"));
                writer.Write(candidate.Code ?? string.Empty);
                writer.Write(candidate.MovementPattern.HasValue);
                writer.Write((int?)candidate.MovementPattern ?? 0);
                writer.Write(candidate.ExerciseType.HasValue);
                writer.Write((int?)candidate.ExerciseType ?? 0);
                writer.Write(candidate.PrimaryMuscleGroup?.Trim().ToLowerInvariant() ?? string.Empty);
                writer.Write(candidate.IsActive);

                WriteSortedStrings(writer, candidate.SecondaryMuscleGroups);
                WriteSortedGuids(writer, candidate.RequiredEquipmentIds);
            }
        }

        private static void WriteAdaptedSession(
            BinaryWriter writer,
            SessionPlanSnapshot adaptedSession)
        {
            writer.Write(adaptedSession.EstimatedDurationSeconds);
            var ordered = adaptedSession.Exercises
                .OrderBy(e => e.Order)
                .ThenBy(e => e.ExerciseCode, StringComparer.Ordinal)
                .ThenBy(e => e.ExerciseId)
                .ToArray();

            writer.Write(ordered.Length);

            foreach (var exercise in ordered)
            {
                writer.Write(exercise.SourceRoutineExerciseId.ToString("N"));
                writer.Write(exercise.ExerciseId.ToString("N"));
                writer.Write(exercise.ExerciseCode ?? string.Empty);
                writer.Write(exercise.Order);
                writer.Write((int)exercise.Priority);
                writer.Write(exercise.MovementPattern.HasValue);
                writer.Write((int?)exercise.MovementPattern ?? 0);
                writer.Write(exercise.ExerciseType.HasValue);
                writer.Write((int?)exercise.ExerciseType ?? 0);
                writer.Write(exercise.PrimaryMuscleGroup?.Trim().ToLowerInvariant() ?? string.Empty);

                writer.Write(exercise.Prescription.Sets);
                writer.Write(exercise.Prescription.MinReps);
                writer.Write(exercise.Prescription.MaxReps);
                writer.Write(exercise.Prescription.MinRir);
                writer.Write(exercise.Prescription.MaxRir);
                writer.Write(exercise.Prescription.RestSeconds);
            }
        }

        private static void WriteChanges(
            BinaryWriter writer,
            IReadOnlyCollection<SessionAdaptationChange> changes)
        {
            var ordered = changes
                .OrderBy(c => c.SourceRoutineExerciseId)
                .ThenBy(c => (int)c.ChangeType)
                .ToArray();

            writer.Write(ordered.Length);

            foreach (var change in ordered)
            {
                writer.Write((int)change.ChangeType);
                writer.Write(change.SourceRoutineExerciseId.ToString("N"));
                writer.Write(change.ExerciseId.ToString("N"));
                writer.Write(change.SubstitutedExerciseId?.ToString("N") ?? string.Empty);
                writer.Write(change.OriginalSets ?? 0);
                writer.Write(change.AdaptedSets ?? 0);
                writer.Write(change.OriginalMinRir ?? 0);
                writer.Write(change.OriginalMaxRir ?? 0);
                writer.Write(change.AdaptedMinRir ?? 0);
                writer.Write(change.AdaptedMaxRir ?? 0);
                writer.Write((int)(change.SubstitutionReason ?? 0));
            }
        }

        private static void WriteDiagnostics(
            BinaryWriter writer,
            IReadOnlyCollection<SessionAdaptationDiagnostic> diagnostics)
        {
            var ordered = diagnostics
                .OrderBy(d => (int)d.Code)
                .ThenBy(d => d.SourceRoutineExerciseId?.ToString("N") ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(d => d.ExerciseId?.ToString("N") ?? string.Empty, StringComparer.Ordinal)
                .ToArray();

            writer.Write(ordered.Length);

            foreach (var diagnostic in ordered)
            {
                writer.Write((int)diagnostic.Code);
                writer.Write(diagnostic.SourceRoutineExerciseId?.ToString("N") ?? string.Empty);
                writer.Write(diagnostic.ExerciseId?.ToString("N") ?? string.Empty);
                writer.Write(diagnostic.SubstitutedExerciseId?.ToString("N") ?? string.Empty);
            }
        }

        private static void WriteSortedStrings(
            BinaryWriter writer,
            IEnumerable<string>? strings)
        {
            var sorted = (strings ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant())
                .Distinct()
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();

            writer.Write(sorted.Length);
            foreach (var str in sorted)
            {
                writer.Write(str);
            }
        }
    }
}

