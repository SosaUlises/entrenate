using System;
using System.Text;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Coach.LiveCoach.Tools;
using Entrenate.Application.Common.Models;

namespace Entrenate.Application.Coach.LiveCoach.Prompt
{
    public interface ILiveCoachPromptBuilder
    {
        CoachConversationPrompt Build(
            LiveCoachSessionContext context,
            string userQuestion,
            PreviousPerformanceData? previousPerformance = null);
    }

    public sealed class LiveCoachPromptBuilder : ILiveCoachPromptBuilder
    {
        private const string SystemInstructions =
            "Sos el Live Coach de Entrenate durante una sesión de entrenamiento activa en el gimnasio.\n" +
            "Directivas obligatorias:\n" +
            "1. Tono e idioma: Hablá en español rioplatense natural (usá voseo: 'te tocan', 'estás', 'hiciste').\n" +
            "2. Brevedad estricta: Respondé directo y conciso, en exactamente 1 a 3 oraciones cortas. Pensá que el usuario está entrenando y necesita una respuesta rápida.\n" +
            "3. Grounding absoluto: Basate ÚNICAMENTE en los datos de la sesión y resultados de herramientas que se te proporcionan a continuación.\n" +
            "4. Cero alucinación: Si un dato no figura en el contexto (por ejemplo, si no hay timer activo o no hay registros anteriores), decilo con naturalidad ('No tenés un timer activo', 'No hay registros previos para este ejercicio'). NUNCA inventes números, pesos ni series.\n" +
            "5. Cero terminología técnica: NUNCA menciones nombres de herramientas (como GetCurrentPrescription), códigos, GUIDs, IDs de base de datos ni detalles de implementación.\n" +
            "6. Cero mutaciones: No podés guardar series, cambiar pesos ni modificar la rutina desde esta interacción.\n" +
            "7. Si la pregunta es sobre técnica o consejos del ejercicio actual, podés dar 1 o 2 indicaciones clave y seguras siempre que sean para el ejercicio que figura en el contexto.";

        public CoachConversationPrompt Build(
            LiveCoachSessionContext context,
            string userQuestion,
            PreviousPerformanceData? previousPerformance = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== CONTEXTO AUTORIZADO DE LA SESIÓN ACTIVA ===");

            if (context.CurrentExercise != null)
            {
                sb.AppendLine($"- Ejercicio actual: {context.CurrentExercise.Name} (orden #{context.CurrentExercise.Order})");
                if (!string.IsNullOrWhiteSpace(context.CurrentExercise.NextExerciseName))
                {
                    sb.AppendLine($"- Siguiente ejercicio en la rutina: {context.CurrentExercise.NextExerciseName}");
                }
                else
                {
                    sb.AppendLine("- Siguiente ejercicio: Este es el último ejercicio programado para la sesión.");
                }
            }
            else
            {
                sb.AppendLine("- Ejercicio actual: No hay ejercicio activo.");
            }

            if (context.CurrentSet != null)
            {
                var targetStr = context.CurrentSet.IsTargetSet ? "serie objetivo" : "serie extra";
                sb.AppendLine($"- Serie actual: Serie #{context.CurrentSet.SetNumber} ({targetStr}) de {context.CurrentSet.TargetSetsPlanned} planificadas.");
            }

            if (context.CurrentPrescription != null)
            {
                sb.AppendLine($"- Prescripción: {context.CurrentPrescription.TargetSets} series de {context.CurrentPrescription.MinRepetitions}-{context.CurrentPrescription.MaxRepetitions} repeticiones, RIR {context.CurrentPrescription.MinRir}-{context.CurrentPrescription.MaxRir}, descanso de {context.CurrentPrescription.RestSeconds}s.");
                if (!string.IsNullOrWhiteSpace(context.CurrentPrescription.Notes))
                {
                    sb.AppendLine($"- Indicaciones del ejercicio: {context.CurrentPrescription.Notes}");
                }
            }

            if (context.CompletedSets.Count > 0)
            {
                sb.AppendLine("- Series ya completadas en este ejercicio:");
                var currentExerciseSets = context.CurrentExercise != null
                    ? context.CompletedSets
                    : context.CompletedSets;

                foreach (var s in currentExerciseSets)
                {
                    if (context.CurrentExercise == null || s.ExerciseName == context.CurrentExercise.Name)
                    {
                        var rirStr = s.Rir.HasValue ? $", RIR {s.Rir.Value}" : string.Empty;
                        sb.AppendLine($"  * Serie {s.SetNumber}: {s.Weight} kg x {s.Repetitions} reps{rirStr}");
                    }
                }
            }
            else
            {
                sb.AppendLine("- Series ya completadas en este ejercicio: Ninguna todavía.");
            }

            if (context.Rest.IsActive && context.Rest.RemainingSecondsAtCapture.HasValue)
            {
                sb.AppendLine($"- Temporizador de descanso: ACTIVO, restan aproximadamente {context.Rest.RemainingSecondsAtCapture.Value} segundos.");
            }
            else
            {
                sb.AppendLine("- Temporizador de descanso: Inactivo.");
            }

            sb.AppendLine($"- Resumen general: {context.Summary.ExerciseCount} ejercicios totales, {context.Summary.CompletedTargetSetCount} de {context.Summary.TargetSetCount} series completadas.");

            if (previousPerformance != null)
            {
                sb.AppendLine("=== HISTORIAL PREVIO DEL EJERCICIO ===");
                if (previousPerformance.Exposures.Count > 0)
                {
                    sb.AppendLine($"Registros anteriores para {previousPerformance.ExerciseName}:");
                    foreach (var exp in previousPerformance.Exposures)
                    {
                        var rirStr = exp.Rir.HasValue ? $", RIR {exp.Rir.Value}" : string.Empty;
                        sb.AppendLine($"  * {exp.SessionDate:yyyy-MM-dd}: {exp.Weight} kg x {exp.Repetitions} reps{rirStr}");
                    }
                }
                else
                {
                    sb.AppendLine($"No hay registros de sesiones anteriores para {previousPerformance.ExerciseName}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(context.DriftNote))
            {
                sb.AppendLine($"=== ACTUALIZACIÓN DE ESTADO RECIENTE ===\n{context.DriftNote}");
            }

            sb.AppendLine("=== CONSULTA DEL USUARIO ===");
            sb.AppendLine(userQuestion.Trim());

            return new CoachConversationPrompt(SystemInstructions, sb.ToString());
        }
    }
}

