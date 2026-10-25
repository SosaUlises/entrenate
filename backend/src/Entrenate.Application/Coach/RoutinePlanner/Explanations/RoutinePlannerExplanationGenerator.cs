using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Explanations
{
    public static class RoutinePlannerExplanationGenerator
    {
        public static RoutinePlannerExplanation Generate(
            RoutinePlannerProposal proposal)
        {
            ArgumentNullException.ThrowIfNull(proposal);

            var days = proposal.Days
                .OrderBy(day => day.Order)
                .ToArray();
            var warnings = BuildWarnings(proposal);

            return new RoutinePlannerExplanation(
                BuildSummary(proposal, warnings.Count),
                BuildStructure(days),
                BuildRationale(proposal, days),
                warnings);
        }

        private static string BuildSummary(
            RoutinePlannerProposal proposal,
            int warningCount)
        {
            if (proposal.Feasibility ==
                RoutineProposalFeasibility.NotFeasible)
            {
                return "No se pudo construir una rutina utilizable con " +
                    "las restricciones actuales.";
            }

            var result = $"Se generó una propuesta de " +
                $"{DescribeCount(proposal.Days.Count, "día", "días")} " +
                $"con una distribución " +
                $"{DescribeSplit(proposal.Days)}.";

            return warningCount == 0
                ? result
                : result + " La propuesta puede revisarse, aunque tiene " +
                    "algunas limitaciones.";
        }

        private static string BuildStructure(
            IReadOnlyCollection<RoutinePlannerProposalDay> days)
        {
            if (days.Count == 0)
            {
                return "No hay sesiones disponibles para organizar en la " +
                    "semana.";
            }

            var calendar = string.Join(
                "; ",
                days.Select(day =>
                    $"{DescribeDay(day.ScheduledDay)}: " +
                    $"{DescribeFocus(day.Focus, day.Variant)} " +
                    $"({DescribeOrigin(day.DayOrigin)})"));
            var preferredCount = days.Count(day =>
                day.DayOrigin == ScheduledDayOrigin.Preferred);
            var derivedCount = days.Count - preferredCount;

            return $"El calendario quedó así: {calendar}. " +
                $"{DescribePreferredDays(preferredCount)} y " +
                $"{DescribeDerivedDays(derivedCount)}.";
        }

        private static string BuildRationale(
            RoutinePlannerProposal proposal,
            IReadOnlyCollection<RoutinePlannerProposalDay> days)
        {
            var exercises = days
                .SelectMany(day => day.Exercises)
                .ToArray();
            var structureReason = DescribeStructureReason(
                proposal.RequestedDaysPerWeek);
            var profileReason = $"El volumen y la prescripción se ajustaron " +
                $"para {DescribeGoal(proposal.Goal)} y un nivel " +
                $"{DescribeExperience(proposal.ExperienceLevel)}.";

            if (exercises.Length == 0)
            {
                return $"{structureReason} {profileReason} No hay una " +
                    "prescripción completa para explicar con las " +
                    "restricciones actuales.";
            }

            var movementPatternCount = exercises
                .Select(exercise => exercise.MovementPattern)
                .Distinct()
                .Count();
            var minEstimatedMinutes = (int)Math.Ceiling(
                days.Min(day => day.EstimatedDurationSeconds) / 60m);
            var maxEstimatedMinutes = (int)Math.Ceiling(
                days.Max(day => day.EstimatedDurationSeconds) / 60m);
            var targetDurations = days
                .Select(day => day.TargetDurationMinutes)
                .Distinct()
                .Order()
                .ToArray();

            return $"{structureReason} {profileReason} La selección cubre " +
                $"{DescribeCount(
                    movementPatternCount,
                    "patrón de movimiento",
                    "patrones de movimiento")} con " +
                $"{DescribeCount(
                    exercises.Length,
                    "ejercicio semanal",
                    "ejercicios semanales")}. " +
                $"{DescribePrescription(exercises)} " +
                $"{DescribeDuration(
                    minEstimatedMinutes,
                    maxEstimatedMinutes,
                    targetDurations)}";
        }

        private static IReadOnlyList<string> BuildWarnings(
            RoutinePlannerProposal proposal)
        {
            var warnings = new List<string>();

            foreach (var diagnostic in proposal.SelectionDiagnostics)
            {
                warnings.Add(DescribeSelectionDiagnostic(diagnostic.Code));
            }

            foreach (var diagnostic in proposal.PrescriptionDiagnostics)
            {
                warnings.Add(DescribePrescriptionDiagnostic(diagnostic.Code));
            }

            return warnings
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string DescribeSelectionDiagnostic(
            ExerciseSelectionDiagnosticCode code)
        {
            return code switch
            {
                ExerciseSelectionDiagnosticCode.MissingRequiredExercise =>
                    "No se encontró una opción compatible para cubrir " +
                    "todos los movimientos necesarios.",
                ExerciseSelectionDiagnosticCode.MissingOptionalExercise =>
                    "No se encontró una opción compatible para uno de los " +
                    "complementos opcionales.",
                ExerciseSelectionDiagnosticCode.ExerciseReusedAcrossVariants =>
                    "Se reutilizó un ejercicio entre sesiones porque no " +
                    "había una alternativa compatible equivalente.",
                _ => throw new ArgumentOutOfRangeException(nameof(code))
            };
        }

        private static string DescribePrescriptionDiagnostic(
            RoutinePrescriptionDiagnosticCode code)
        {
            return code switch
            {
                RoutinePrescriptionDiagnosticCode.SelectionNotFeasible =>
                    "La selección no cubre todos los ejercicios necesarios " +
                    "para completar la rutina.",
                RoutinePrescriptionDiagnosticCode
                    .WeeklyVolumeTargetUnreachable =>
                    "No se pudo alcanzar todo el volumen semanal previsto " +
                    "con las restricciones actuales.",
                RoutinePrescriptionDiagnosticCode
                    .OptionalExerciseRemovedForDuration =>
                    "Se retiraron complementos opcionales para respetar el " +
                    "tiempo disponible.",
                RoutinePrescriptionDiagnosticCode
                    .ExerciseSetsReducedForDuration =>
                    "Se redujeron algunas series para ajustar las sesiones " +
                    "al tiempo disponible.",
                RoutinePrescriptionDiagnosticCode.SessionDurationExceeded =>
                    "Al menos una sesión supera la duración objetivo incluso " +
                    "después de los ajustes.",
                _ => throw new ArgumentOutOfRangeException(nameof(code))
            };
        }

        private static string DescribeSplit(
            IReadOnlyCollection<RoutinePlannerProposalDay> days)
        {
            var focuses = days.Select(day => day.Focus).ToArray();

            if (focuses.All(focus => focus == RoutineSessionFocus.FullBody))
            {
                return "de cuerpo completo";
            }

            if (focuses.Length == 4 &&
                focuses.Count(focus => focus == RoutineSessionFocus.Upper) == 2 &&
                focuses.Count(focus => focus == RoutineSessionFocus.Lower) == 2)
            {
                return "de tren superior y tren inferior";
            }

            if (focuses.Contains(RoutineSessionFocus.Upper) &&
                focuses.Contains(RoutineSessionFocus.Lower) &&
                focuses.Contains(RoutineSessionFocus.Push) &&
                focuses.Contains(RoutineSessionFocus.Pull) &&
                focuses.Contains(RoutineSessionFocus.Legs))
            {
                return "combinada de tren superior/inferior y " +
                    "empuje/tracción/piernas";
            }

            if (focuses.Contains(RoutineSessionFocus.CoreCalves))
            {
                return "de empuje/tracción/piernas con una sesión corta " +
                    "de core y gemelos";
            }

            return "de empuje, tracción y piernas";
        }

        private static string DescribeStructureReason(int daysPerWeek)
        {
            return daysPerWeek switch
            {
                1 => "Con un día semanal, una sesión de cuerpo completo " +
                    "permite cubrir todos los focos en una sola jornada.",
                2 or 3 => $"Con {daysPerWeek} días semanales, las sesiones " +
                    "de cuerpo completo distribuyen los focos durante la " +
                    "semana.",
                4 => "Con cuatro días semanales, alternar tren superior e " +
                    "inferior distribuye cada foco en dos sesiones.",
                5 => "Con cinco días semanales, la estructura combina tren " +
                    "superior/inferior con empuje, tracción y piernas.",
                6 => "Con seis días semanales, empuje, tracción y piernas " +
                    "se repiten en variantes A y B.",
                7 => "Con siete días semanales, empuje, tracción y piernas " +
                    "se repiten en variantes A y B, más una sesión corta " +
                    "de core y gemelos.",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(daysPerWeek))
            };
        }

        private static string DescribePrescription(
            IReadOnlyCollection<RoutinePlannerProposalExercise> exercises)
        {
            return $"La prescripción usa entre {exercises.Min(x => x.Sets)} " +
                $"y {exercises.Max(x => x.Sets)} series por ejercicio, " +
                $"rangos de {exercises.Min(x => x.MinReps)} a " +
                $"{exercises.Max(x => x.MaxReps)} repeticiones, RIR de " +
                $"{exercises.Min(x => x.MinRir)} a " +
                $"{exercises.Max(x => x.MaxRir)} y descansos de " +
                $"{exercises.Min(x => x.RestSeconds)} a " +
                $"{exercises.Max(x => x.RestSeconds)} segundos.";
        }

        private static string DescribeDuration(
            int minEstimatedMinutes,
            int maxEstimatedMinutes,
            IReadOnlyCollection<int> targetDurations)
        {
            var estimated = minEstimatedMinutes == maxEstimatedMinutes
                ? $"{minEstimatedMinutes} minutos"
                : $"entre {minEstimatedMinutes} y {maxEstimatedMinutes} minutos";
            var target = targetDurations.Count == 1
                ? $"{targetDurations.Single()} minutos"
                : $"entre {targetDurations.Min()} y " +
                    $"{targetDurations.Max()} minutos";

            return $"Las sesiones se estiman en {estimated}, con una " +
                $"disponibilidad de {target}.";
        }

        private static string DescribeFocus(
            RoutineSessionFocus focus,
            RoutineSessionVariant variant)
        {
            var description = focus switch
            {
                RoutineSessionFocus.FullBody => "cuerpo completo",
                RoutineSessionFocus.Upper => "tren superior",
                RoutineSessionFocus.Lower => "tren inferior",
                RoutineSessionFocus.Push => "empuje",
                RoutineSessionFocus.Pull => "tracción",
                RoutineSessionFocus.Legs => "piernas",
                RoutineSessionFocus.CoreCalves => "core y gemelos",
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            return variant == RoutineSessionVariant.None
                ? description
                : $"{description} {variant}";
        }

        private static string DescribeOrigin(ScheduledDayOrigin origin)
        {
            return origin switch
            {
                ScheduledDayOrigin.Preferred => "elegido",
                ScheduledDayOrigin.Derived => "inferido",
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
        }

        private static string DescribeDay(DiaSemana day)
        {
            return day switch
            {
                DiaSemana.Lunes => "lunes",
                DiaSemana.Martes => "martes",
                DiaSemana.Miercoles => "miércoles",
                DiaSemana.Jueves => "jueves",
                DiaSemana.Viernes => "viernes",
                DiaSemana.Sabado => "sábado",
                DiaSemana.Domingo => "domingo",
                _ => throw new ArgumentOutOfRangeException(nameof(day))
            };
        }

        private static string DescribeGoal(ObjetivoEntrenamiento goal)
        {
            return goal switch
            {
                ObjetivoEntrenamiento.GanarMasaMuscular =>
                    "ganar masa muscular",
                ObjetivoEntrenamiento.GanarFuerza => "ganar fuerza",
                ObjetivoEntrenamiento.GanarmasaMuscularYFuerza =>
                    "ganar masa muscular y fuerza",
                ObjetivoEntrenamiento.AcondicionamientoGeneral =>
                    "acondicionamiento general",
                _ => throw new ArgumentOutOfRangeException(nameof(goal))
            };
        }

        private static string DescribeExperience(NivelExperiencia experience)
        {
            return experience switch
            {
                NivelExperiencia.SinExperiencia => "inicial",
                NivelExperiencia.Principiante => "principiante",
                NivelExperiencia.Intermedio => "intermedio",
                NivelExperiencia.Avanzado => "avanzado",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(experience))
            };
        }

        private static string DescribeCount(
            int count,
            string singular,
            string plural)
        {
            return $"{count} {(count == 1 ? singular : plural)}";
        }

        private static string DescribePreferredDays(int count)
        {
            return count switch
            {
                0 => "No había días elegidos",
                1 => "Se conservó 1 día elegido",
                _ => $"Se conservaron {count} días elegidos"
            };
        }

        private static string DescribeDerivedDays(int count)
        {
            return count switch
            {
                0 => "no fue necesario inferir días",
                1 => "se infirió 1 día",
                _ => $"se infirieron {count} días"
            };
        }
    }
}
