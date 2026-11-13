using System;
using System.Collections.Generic;
using System.Linq;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Coach.LiveCoach.Routing;

namespace Entrenate.Application.Coach.LiveCoach.Rules
{
    public static class LiveCoachCurrentPrescriptionQueryResolver
    {
        private const string SessionCompleteMessage = "Ya completaste todas las series de la sesión.";

        public static string Resolve(
            LiveCoachIntentKind intent,
            LiveCoachSessionContext context,
            LiveCoachPrescriptionContext? prescription)
        {
            if (context.CurrentExercise is null || context.CurrentSet is null)
                return SessionCompleteMessage;

            if (intent == LiveCoachIntentKind.CurrentWeightQuery)
                return "No tenés un peso objetivo definido para esta serie.";

            if (prescription is null)
                return SessionCompleteMessage;

            return intent switch
            {
                LiveCoachIntentKind.CurrentRepetitionsQuery => FormatRepetitionsAnswer(prescription),
                LiveCoachIntentKind.CurrentRirQuery => FormatRirAnswer(prescription),
                LiveCoachIntentKind.CurrentRestQuery => FormatRestAnswer(prescription.RestSeconds),
                LiveCoachIntentKind.CurrentPrescriptionQuery => FormatFullPrescription(context, prescription),
                _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Consulta de prescripción no soportada.")
            };
        }

        private static string FormatRepetitionsAnswer(LiveCoachPrescriptionContext prescription)
        {
            return prescription.MinRepetitions == prescription.MaxRepetitions
                ? $"Te tocan {FormatCount(prescription.MinRepetitions, "repetición", "repeticiones")}."
                : $"Te tocan entre {prescription.MinRepetitions} y {prescription.MaxRepetitions} repeticiones.";
        }

        private static string FormatRirAnswer(LiveCoachPrescriptionContext prescription)
        {
            return prescription.MinRir == prescription.MaxRir
                ? $"Te toca RIR {prescription.MinRir}."
                : $"Te toca RIR entre {prescription.MinRir} y {prescription.MaxRir}.";
        }

        private static string FormatRestAnswer(int restSeconds)
        {
            if (restSeconds <= 0)
                return "Esta serie no tiene un descanso objetivo definido.";

            if (restSeconds % 60 == 0)
            {
                var minutes = restSeconds / 60;
                return minutes == 1
                    ? "Te toca descansar 1 minuto."
                    : $"Te tocan {minutes} minutos de descanso.";
            }

            return $"Te toca descansar {FormatDuration(restSeconds)}.";
        }

        private static string FormatFullPrescription(
            LiveCoachSessionContext context,
            LiveCoachPrescriptionContext prescription)
        {
            var parts = new List<string>
            {
                prescription.MinRepetitions == prescription.MaxRepetitions
                    ? FormatCount(prescription.MinRepetitions, "repetición", "repeticiones")
                    : $"entre {prescription.MinRepetitions} y {prescription.MaxRepetitions} repeticiones",
                prescription.MinRir == prescription.MaxRir
                    ? $"RIR {prescription.MinRir}"
                    : $"RIR entre {prescription.MinRir} y {prescription.MaxRir}"
            };

            if (prescription.RestSeconds > 0)
                parts.Add($"{FormatDuration(prescription.RestSeconds)} de descanso");

            return $"{context.CurrentExercise!.Name}, serie {context.CurrentSet!.SetNumber}: {JoinNaturally(parts)}.";
        }

        private static string FormatDuration(int totalSeconds)
        {
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            if (minutes == 0)
                return FormatCount(seconds, "segundo", "segundos");
            if (seconds == 0)
                return FormatCount(minutes, "minuto", "minutos");

            return $"{FormatCount(minutes, "minuto", "minutos")} y {FormatCount(seconds, "segundo", "segundos")}";
        }

        private static string FormatCount(int value, string singular, string plural)
            => $"{value} {(value == 1 ? singular : plural)}";

        private static string JoinNaturally(IReadOnlyList<string> parts)
        {
            return parts.Count switch
            {
                0 => string.Empty,
                1 => parts[0],
                2 => $"{parts[0]} y {parts[1]}",
                _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} y {parts[^1]}"
            };
        }
    }
}
