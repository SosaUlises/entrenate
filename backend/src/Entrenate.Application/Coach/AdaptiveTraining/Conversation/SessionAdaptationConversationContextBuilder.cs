using Entrenate.Application.Coach.AdaptiveTraining.Explanations;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;

namespace Entrenate.Application.Coach.AdaptiveTraining.Conversation
{
    public static class SessionAdaptationConversationContextBuilder
    {
        public static SessionAdaptationConversationContext Build(
            SessionAdaptationProposal proposal,
            SessionAdaptationExplanation? explanation = null)
        {
            ArgumentNullException.ThrowIfNull(proposal);

            var effectiveExplanation = explanation ?? SessionAdaptationExplanationGenerator.Generate(proposal);

            var originalBySource = proposal.OriginalSession.Exercises
                .ToDictionary(e => e.SourceRoutineExerciseId);
            var adaptedBySource = proposal.AdaptedSession.Exercises
                .ToDictionary(e => e.SourceRoutineExerciseId);
            var originalById = proposal.OriginalSession.Exercises
                .ToDictionary(e => e.ExerciseId);
            var adaptedById = proposal.AdaptedSession.Exercises
                .ToDictionary(e => e.ExerciseId);

            var origMinutes = (int)Math.Ceiling(proposal.OriginalSession.EstimatedDurationSeconds / 60m);
            var adaptedMinutes = (int)Math.Ceiling(proposal.AdaptedSession.EstimatedDurationSeconds / 60m);

            var originalExercises = proposal.OriginalSession.Exercises
                .OrderBy(e => e.Order)
                .Select(MapExercise)
                .ToList();

            var adaptedExercises = proposal.AdaptedSession.Exercises
                .OrderBy(e => e.Order)
                .Select(MapExercise)
                .ToList();

            var orderedChanges = proposal.Changes
                .OrderBy(GetChangeTypeSortOrder)
                .ThenBy(c => GetExerciseOrder(c, originalBySource, originalById))
                .ThenBy(c => GetOriginalExerciseName(c, originalBySource, originalById), StringComparer.Ordinal)
                .ToArray();

            var changes = orderedChanges
                .Select(c => MapChange(c, originalBySource, adaptedBySource, originalById, adaptedById))
                .ToList();

            return new SessionAdaptationConversationContext(
                DescribeFeasibility(proposal.Feasibility),
                proposal.Feasibility != SessionAdaptationFeasibility.NotFeasible,
                proposal.Context.AvailableMinutes,
                DescribeEffortPreference(proposal.Context.EffortPreference),
                origMinutes,
                adaptedMinutes,
                originalExercises,
                adaptedExercises,
                changes,
                effectiveExplanation.Warnings,
                effectiveExplanation);
        }

        private static SessionAdaptationConversationExercise MapExercise(SessionExerciseSnapshot exercise)
        {
            return new SessionAdaptationConversationExercise(
                exercise.Name,
                exercise.Order,
                DescribePriority(exercise.Priority),
                exercise.Prescription.Sets,
                exercise.Prescription.MinReps,
                exercise.Prescription.MaxReps,
                exercise.Prescription.MinRir,
                exercise.Prescription.MaxRir,
                exercise.Prescription.RestSeconds,
                exercise.Prescription.Notes);
        }

        private static SessionAdaptationConversationChange MapChange(
            SessionAdaptationChange change,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalBySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> adaptedBySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalById,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> adaptedById)
        {
            var origName = GetOriginalExerciseName(change, originalBySource, originalById);
            var adaptedName = change.ChangeType == SessionAdaptationChangeType.ExerciseSubstituted
                ? GetAdaptedExerciseName(change, adaptedBySource, adaptedById)
                : null;

            var (category, description, reason) = DescribeChangeDetails(change, origName, adaptedName);

            return new SessionAdaptationConversationChange(
                description,
                category,
                origName,
                adaptedName,
                change.OriginalSets,
                change.AdaptedSets,
                change.OriginalMinRir,
                change.OriginalMaxRir,
                change.AdaptedMinRir,
                change.AdaptedMaxRir,
                reason);
        }

        private static (string Category, string Description, string Reason) DescribeChangeDetails(
            SessionAdaptationChange change,
            string origName,
            string? adaptedName)
        {
            switch (change.ChangeType)
            {
                case SessionAdaptationChangeType.ExerciseSubstituted:
                    var subReason = DescribeSubstitutionReason(change.SubstitutionReason);
                    return (
                        "Sustitución",
                        $"Se sustituyó '{origName}' por '{adaptedName}' ({subReason}).",
                        subReason);

                case SessionAdaptationChangeType.OptionalAccessoryRemovedNoSubstitute:
                    return (
                        "Sustitución",
                        $"Se retiró el accesorio '{origName}' por falta de sustituto compatible.",
                        "Falta de sustituto compatible");

                case SessionAdaptationChangeType.RequiredExerciseUnavailable:
                    return (
                        "Sustitución",
                        $"El ejercicio requerido '{origName}' no tiene sustituto disponible.",
                        "Ejercicio requerido sin sustituto");

                case SessionAdaptationChangeType.ExerciseSetsReducedForTime:
                    return (
                        "Tiempo",
                        $"Se redujeron las series de '{origName}' de {change.OriginalSets} a {change.AdaptedSets} por tiempo.",
                        "Ajuste al tiempo disponible");

                case SessionAdaptationChangeType.OptionalAccessoryRemovedForTime:
                    return (
                        "Tiempo",
                        $"Se retiró el accesorio '{origName}' para cumplir con el tiempo disponible.",
                        "Ajuste al tiempo disponible");

                case SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter:
                    return (
                        "Esfuerzo",
                        $"Se redujo una serie en '{origName}' (de {change.OriginalSets} a {change.AdaptedSets}) por modo liviano.",
                        "Modo liviano");

                case SessionAdaptationChangeType.RirIncreased:
                    return (
                        "Esfuerzo",
                        $"Se aumentó el RIR de '{origName}' de {change.OriginalMinRir}-{change.OriginalMaxRir} a {change.AdaptedMinRir}-{change.AdaptedMaxRir}.",
                        "Modo liviano");

                default:
                    return (
                        "Ajuste",
                        $"Ajuste aplicado en '{origName}'.",
                        "Adaptación de sesión");
            }
        }

        private static string DescribeSubstitutionReason(AdaptiveSubstitutionReason? reason)
        {
            if (reason is null || reason == AdaptiveSubstitutionReason.None)
            {
                return "Restricción de sesión";
            }

            var isEquip = reason.Value.HasFlag(AdaptiveSubstitutionReason.EquipmentUnavailable);
            var isExcl = reason.Value.HasFlag(AdaptiveSubstitutionReason.UserExcludedExercise);

            if (isEquip && isExcl)
            {
                return "Equipamiento no disponible y ejercicio excluido";
            }

            if (isEquip)
            {
                return "Equipamiento no disponible";
            }

            if (isExcl)
            {
                return "Ejercicio excluido";
            }

            return "Restricción de sesión";
        }

        private static string DescribeFeasibility(SessionAdaptationFeasibility feasibility) => feasibility switch
        {
            SessionAdaptationFeasibility.Feasible => "Factible",
            SessionAdaptationFeasibility.FeasibleWithChanges => "Factible con adaptaciones",
            SessionAdaptationFeasibility.NotFeasible => "No factible",
            _ => "Desconocida"
        };

        private static string DescribeEffortPreference(SessionEffortPreference preference) => preference switch
        {
            SessionEffortPreference.Lighter => "Más liviana",
            _ => "Normal"
        };

        private static string DescribePriority(AdaptiveTrainingExercisePriority priority) => priority switch
        {
            AdaptiveTrainingExercisePriority.RequiredCompound => "Compuesto principal requerido",
            AdaptiveTrainingExercisePriority.RequiredIsolation => "Aislamiento principal requerido",
            AdaptiveTrainingExercisePriority.OptionalAccessory => "Accesorio opcional",
            AdaptiveTrainingExercisePriority.UnclassifiedRequired => "Ejercicio requerido",
            _ => "Ejercicio"
        };

        private static int GetChangeTypeSortOrder(SessionAdaptationChange change) => change.ChangeType switch
        {
            SessionAdaptationChangeType.ExerciseSubstituted => 1,
            SessionAdaptationChangeType.OptionalAccessoryRemovedNoSubstitute => 2,
            SessionAdaptationChangeType.RequiredExerciseUnavailable => 3,
            SessionAdaptationChangeType.ExerciseSetsReducedForTime => 4,
            SessionAdaptationChangeType.OptionalAccessoryRemovedForTime => 5,
            SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter => 6,
            SessionAdaptationChangeType.RirIncreased => 7,
            _ => 99
        };

        private static int GetExerciseOrder(
            SessionAdaptationChange change,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> bySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> byId)
        {
            if (bySource.TryGetValue(change.SourceRoutineExerciseId, out var orig))
            {
                return orig.Order;
            }
            if (byId.TryGetValue(change.ExerciseId, out var origById))
            {
                return origById.Order;
            }
            return 999;
        }

        private static string GetOriginalExerciseName(
            SessionAdaptationChange change,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> bySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> byId)
        {
            if (bySource.TryGetValue(change.SourceRoutineExerciseId, out var orig))
            {
                return orig.Name;
            }
            if (byId.TryGetValue(change.ExerciseId, out var origById))
            {
                return origById.Name;
            }
            return change.ExerciseCode ?? "Ejercicio";
        }

        private static string GetAdaptedExerciseName(
            SessionAdaptationChange change,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> bySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> byId)
        {
            if (bySource.TryGetValue(change.SourceRoutineExerciseId, out var adapted))
            {
                return adapted.Name;
            }
            if (change.SubstitutedExerciseId.HasValue &&
                byId.TryGetValue(change.SubstitutedExerciseId.Value, out var adaptedById))
            {
                return adaptedById.Name;
            }
            return change.SubstitutedExerciseCode ?? change.ExerciseCode ?? "ejercicio sustituto";
        }
    }
}

