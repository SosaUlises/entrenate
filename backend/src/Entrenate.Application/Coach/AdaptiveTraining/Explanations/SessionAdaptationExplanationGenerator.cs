using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;

namespace Entrenate.Application.Coach.AdaptiveTraining.Explanations
{
    public static class SessionAdaptationExplanationGenerator
    {
        public static SessionAdaptationExplanation Generate(
            SessionAdaptationProposal proposal)
        {
            ArgumentNullException.ThrowIfNull(proposal);

            var originalBySource = proposal.OriginalSession.Exercises
                .ToDictionary(e => e.SourceRoutineExerciseId);
            var adaptedBySource = proposal.AdaptedSession.Exercises
                .ToDictionary(e => e.SourceRoutineExerciseId);
            var originalById = proposal.OriginalSession.Exercises
                .ToDictionary(e => e.ExerciseId);
            var adaptedById = proposal.AdaptedSession.Exercises
                .ToDictionary(e => e.ExerciseId);

            var orderedChanges = proposal.Changes
                .OrderBy(GetChangeTypeSortOrder)
                .ThenBy(c => GetExerciseOrder(c, originalBySource, originalById))
                .ThenBy(c => GetOriginalExerciseName(c, originalBySource, originalById), StringComparer.Ordinal)
                .ToArray();

            var changesExplanations = orderedChanges
                .Select(c => DescribeChange(c, originalBySource, adaptedBySource, originalById, adaptedById))
                .ToList();

            var warnings = BuildWarnings(proposal, originalBySource, originalById);
            var summary = BuildSummary(proposal, orderedChanges);
            var rationale = BuildRationale(proposal, orderedChanges, originalBySource, originalById);

            return new SessionAdaptationExplanation(
                summary,
                changesExplanations,
                rationale,
                warnings);
        }

        private static string BuildSummary(
            SessionAdaptationProposal proposal,
            IReadOnlyCollection<SessionAdaptationChange> changes)
        {
            if (proposal.Feasibility == SessionAdaptationFeasibility.NotFeasible)
            {
                return "La sesión no puede adaptarse respetando las restricciones y reglas actuales.";
            }

            if (proposal.Feasibility == SessionAdaptationFeasibility.Feasible && changes.Count == 0)
            {
                return "No fue necesario adaptar la sesión; la planificación original es completamente realizable con los recursos disponibles.";
            }

            var hasSubstitutions = changes.Any(c => c.ChangeType is
                SessionAdaptationChangeType.ExerciseSubstituted or
                SessionAdaptationChangeType.OptionalAccessoryRemovedNoSubstitute or
                SessionAdaptationChangeType.RequiredExerciseUnavailable);

            var hasTime = changes.Any(c => c.ChangeType is
                SessionAdaptationChangeType.ExerciseSetsReducedForTime or
                SessionAdaptationChangeType.OptionalAccessoryRemovedForTime);

            var hasLighter = changes.Any(c => c.ChangeType is
                SessionAdaptationChangeType.RirIncreased or
                SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter);

            var dimensionsCount = (hasSubstitutions ? 1 : 0) + (hasTime ? 1 : 0) + (hasLighter ? 1 : 0);

            if (dimensionsCount > 1)
            {
                var parts = new List<string>();
                if (hasSubstitutions) parts.Add("sustitución de ejercicios");
                if (hasTime) parts.Add("ajuste al tiempo disponible");
                if (hasLighter) parts.Add("modo más liviano");

                return $"Se combinaron varias adaptaciones ({string.Join(", ", parts)}) para ajustar la sesión a las condiciones disponibles.";
            }

            if (hasSubstitutions)
            {
                return "Se reemplazaron ejercicios para adaptarse a la disponibilidad de equipamiento y exclusiones solicitadas.";
            }

            if (hasTime)
            {
                return "Se ajustó el volumen de la sesión al tiempo disponible, preservando los ejercicios principales requeridos.";
            }

            if (hasLighter)
            {
                return "Se adaptó la sesión a un modo más liviano aumentando el RIR objetivo para reducir la exigencia global.";
            }

            return "Se adaptó la sesión según las preferencias y condiciones indicadas.";
        }

        private static string BuildRationale(
            SessionAdaptationProposal proposal,
            IReadOnlyCollection<SessionAdaptationChange> changes,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalBySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalById)
        {
            var origMinutes = (int)Math.Ceiling(proposal.OriginalSession.EstimatedDurationSeconds / 60m);
            var adaptedMinutes = (int)Math.Ceiling(proposal.AdaptedSession.EstimatedDurationSeconds / 60m);

            var rationaleParts = new List<string>();

            if (proposal.Context.AvailableMinutes.HasValue)
            {
                rationaleParts.Add(
                    $"La sesión original requería aproximadamente {origMinutes} minutos y se adaptó a aproximadamente {adaptedMinutes} minutos para respetar el límite de {proposal.Context.AvailableMinutes.Value} minutos disponibles (tiempo máximo disponible para entrenar hoy, no una duración obligatoria a completar).");
            }
            else
            {
                rationaleParts.Add(
                    $"La sesión adaptada tiene una duración estimada de aproximadamente {adaptedMinutes} minutos (duración original: {origMinutes} minutos).");
            }

            var hasSubstitutions = changes.Any(c => c.ChangeType == SessionAdaptationChangeType.ExerciseSubstituted);
            if (hasSubstitutions)
            {
                rationaleParts.Add(
                    "Las sustituciones de ejercicios conservan el patrón de movimiento, el grupo muscular principal y el tipo de ejercicio para mantener el estímulo planificado con el equipamiento accesible.");
            }

            var hasTimeReductions = changes.Any(c => c.ChangeType is
                SessionAdaptationChangeType.ExerciseSetsReducedForTime or
                SessionAdaptationChangeType.OptionalAccessoryRemovedForTime);
            if (hasTimeReductions)
            {
                rationaleParts.Add(
                    "Se priorizó conservar el trabajo principal y los ejercicios requeridos, aplicando los recortes de volumen exclusivamente sobre accesorios opcionales.");
            }

            var hasLighter = changes.Any(c => c.ChangeType is
                SessionAdaptationChangeType.RirIncreased or
                SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter);
            if (hasLighter)
            {
                rationaleParts.Add(
                    "El modo liviano incrementa el margen de repeticiones en reserva (RIR) para disminuir la exigencia global de la sesión, sin alterar las cargas ni los descansos prescritos.");
            }

            if (proposal.Feasibility == SessionAdaptationFeasibility.NotFeasible)
            {
                var blockers = new List<string>();
                if (proposal.Diagnostics.Any(d => d.Code == SessionAdaptationDiagnosticCode.RequiredExerciseHasNoSubstitute))
                {
                    blockers.Add("no existe un reemplazo válido con las restricciones actuales para uno o más ejercicios requeridos");
                }
                if (proposal.Diagnostics.Any(d => d.Code == SessionAdaptationDiagnosticCode.MinimumSessionExceedsAvailableTime))
                {
                    blockers.Add($"incluso la versión mínima permitida de la sesión supera el tiempo disponible ({proposal.Context.AvailableMinutes} minutos)");
                }

                if (blockers.Count > 0)
                {
                    rationaleParts.Add(
                        $"La sesión no puede confirmarse debido a que {string.Join(" y ", blockers)}.");
                }
            }

            return string.Join(" ", rationaleParts);
        }

        private static string DescribeChange(
            SessionAdaptationChange change,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalBySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> adaptedBySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalById,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> adaptedById)
        {
            var origName = GetOriginalExerciseName(change, originalBySource, originalById);
            var adaptedName = GetAdaptedExerciseName(change, adaptedBySource, adaptedById);

            return change.ChangeType switch
            {
                SessionAdaptationChangeType.ExerciseSubstituted =>
                    $"Se reemplazó '{origName}' por '{adaptedName}' {DescribeSubstitutionReason(change.SubstitutionReason)}, conservando el patrón de movimiento, grupo muscular principal y tipo de ejercicio.",

                SessionAdaptationChangeType.OptionalAccessoryRemovedNoSubstitute =>
                    $"Se retiró el accesorio opcional '{origName}' al no existir un reemplazo válido con las restricciones actuales.",

                SessionAdaptationChangeType.RequiredExerciseUnavailable =>
                    $"El ejercicio requerido '{origName}' no puede realizarse y no existe un reemplazo válido con las restricciones actuales.",

                SessionAdaptationChangeType.ExerciseSetsReducedForTime =>
                    $"Se redujeron las series de '{origName}' de {change.OriginalSets} a {change.AdaptedSets} para ajustar la duración de la sesión al tiempo disponible.",

                SessionAdaptationChangeType.OptionalAccessoryRemovedForTime =>
                    $"Se retiró el accesorio opcional '{origName}' para ajustar la duración de la sesión al tiempo disponible, priorizando el trabajo principal.",

                SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter =>
                    $"Se redujo una serie en el accesorio opcional '{origName}' (de {change.OriginalSets} a {change.AdaptedSets}) para disminuir la exigencia global en modo liviano.",

                SessionAdaptationChangeType.RirIncreased =>
                    $"Se incrementó el RIR objetivo de '{origName}' de {change.OriginalMinRir}-{change.OriginalMaxRir} a {change.AdaptedMinRir}-{change.AdaptedMaxRir} para una sesión menos exigente.",

                _ => $"Se aplicó un ajuste en '{origName}'."
            };
        }

        private static IReadOnlyList<string> BuildWarnings(
            SessionAdaptationProposal proposal,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalBySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> originalById)
        {
            var warnings = new List<string>();

            var orderedDiagnostics = proposal.Diagnostics
                .OrderBy(GetDiagnosticSortOrder)
                .ThenBy(d => GetDiagnosticExerciseOrder(d, originalBySource, originalById))
                .ToArray();

            foreach (var diag in orderedDiagnostics)
            {
                var name = GetDiagnosticExerciseName(diag, originalBySource, originalById);

                switch (diag.Code)
                {
                    case SessionAdaptationDiagnosticCode.RequiredExerciseHasNoSubstitute:
                        warnings.Add($"El ejercicio requerido '{name}' no cuenta con un reemplazo válido con las restricciones actuales. La sesión no puede confirmarse.");
                        break;

                    case SessionAdaptationDiagnosticCode.MinimumSessionExceedsAvailableTime:
                        warnings.Add($"Incluso la versión mínima permitida de la sesión supera el tiempo disponible ({proposal.Context.AvailableMinutes} minutos).");
                        break;

                    case SessionAdaptationDiagnosticCode.OptionalExerciseRemovedNoSubstitute:
                        warnings.Add($"El accesorio opcional '{name}' fue retirado al no contar con un reemplazo válido con las restricciones actuales.");
                        break;

                    case SessionAdaptationDiagnosticCode.OptionalAccessoryRemovedForTime:
                        warnings.Add($"El accesorio opcional '{name}' fue eliminado para no exceder el tiempo disponible.");
                        break;

                    case SessionAdaptationDiagnosticCode.NeutralLighterTransformation:
                        warnings.Add("La sesión ya se encontraba en el RIR máximo permitido (5) y con series mínimas, por lo que el modo liviano no aplicó modificaciones adicionales.");
                        break;
                }
            }

            return warnings.Distinct().ToList();
        }

        private static string DescribeSubstitutionReason(AdaptiveSubstitutionReason? reason)
        {
            if (reason is null || reason == AdaptiveSubstitutionReason.None)
            {
                return "por restricciones de la sesión";
            }

            var isEquip = reason.Value.HasFlag(AdaptiveSubstitutionReason.EquipmentUnavailable);
            var isExcl = reason.Value.HasFlag(AdaptiveSubstitutionReason.UserExcludedExercise);

            if (isEquip && isExcl)
            {
                return "porque fue excluido para esta sesión y su equipamiento tampoco está disponible";
            }

            if (isEquip)
            {
                return "porque el equipamiento necesario no está disponible en esta sesión";
            }

            if (isExcl)
            {
                return "porque decidiste no realizarlo en esta sesión";
            }

            return "por restricciones de la sesión";
        }

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

        private static int GetDiagnosticSortOrder(SessionAdaptationDiagnostic diag) => diag.Code switch
        {
            SessionAdaptationDiagnosticCode.RequiredExerciseHasNoSubstitute => 1,
            SessionAdaptationDiagnosticCode.MinimumSessionExceedsAvailableTime => 2,
            SessionAdaptationDiagnosticCode.OptionalExerciseRemovedNoSubstitute => 3,
            SessionAdaptationDiagnosticCode.OptionalAccessoryRemovedForTime => 4,
            SessionAdaptationDiagnosticCode.NeutralLighterTransformation => 5,
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

        private static int GetDiagnosticExerciseOrder(
            SessionAdaptationDiagnostic diag,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> bySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> byId)
        {
            if (diag.SourceRoutineExerciseId.HasValue &&
                bySource.TryGetValue(diag.SourceRoutineExerciseId.Value, out var orig))
            {
                return orig.Order;
            }
            if (diag.ExerciseId.HasValue &&
                byId.TryGetValue(diag.ExerciseId.Value, out var origById))
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

        private static string GetDiagnosticExerciseName(
            SessionAdaptationDiagnostic diag,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> bySource,
            IReadOnlyDictionary<Guid, SessionExerciseSnapshot> byId)
        {
            if (diag.SourceRoutineExerciseId.HasValue &&
                bySource.TryGetValue(diag.SourceRoutineExerciseId.Value, out var orig))
            {
                return orig.Name;
            }
            if (diag.ExerciseId.HasValue &&
                byId.TryGetValue(diag.ExerciseId.Value, out var origById))
            {
                return origById.Name;
            }
            return diag.ExerciseCode ?? "ejercicio";
        }
    }
}

