using System;
using System.Collections.Generic;

namespace Entrenate.Application.Coach.LiveCoach.Routing
{
    public enum LiveCoachIntentKind
    {
        ConversationalQuestion,
        DataQuery,
        MutationRequest,
        OutOfScope
    }

    public sealed record LiveCoachRoutingResult(
        LiveCoachIntentKind Kind,
        bool RequiresPreviousPerformance,
        string? DirectAnswer = null);

    public interface ILiveCoachIntentRouter
    {
        LiveCoachRoutingResult Route(string question);
    }

    public sealed class LiveCoachIntentRouter : ILiveCoachIntentRouter
    {
        private static readonly string[] MutationKeywords =
        {
            "anotá", "anota", "anotame", "anotale",
            "guardá", "guarda", "guardame",
            "registrá", "registra", "registrame",
            "cambiá", "cambia", "cambiame",
            "modificá", "modifica", "modificame",
            "sacá", "saca", "sacame",
            "borrá", "borra", "borrame",
            "eliminá", "elimina", "eliminame",
            "terminá", "terminar", "terminame",
            "cancelá", "cancelar", "cancelame",
            "completá", "completa", "completame",
            "adaptá", "adaptar", "adaptame"
        };

        private static readonly string[] OutOfScopeKeywords =
        {
            "dieta", "caloría", "caloria", "calorías", "calorias", "comida",
            "comer", "proteína", "proteina", "suplemento", "creatina", "almuerzo",
            "cena", "desayuno", "dolor", "lesión", "lesion", "médico", "medico",
            "pastilla", "medicamento", "lastimé", "lastime", "desgarro", "chiste",
            "clima", "canción", "cancion", "película", "pelicula", "receta"
        };

        private static readonly string[] HistoryKeywords =
        {
            "anterior", "pasada", "última vez", "ultima vez", "peso levanté",
            "peso levante", "historial", "récord", "record", "sesión previa"
        };

        private static readonly string[] DataQueryKeywords =
        {
            "cuántas", "cuantas", "cuánto", "cuanto", "cuál", "cual",
            "qué serie", "que serie", "qué ejercicio", "que ejercicio",
            "siguiente", "sigue", "después", "despues", "descanso", "timer",
            "reloj", "reps", "repeticiones", "rir", "prescripción", "prescripcion",
            "peso", "resumen", "cómo vengo", "como vengo", "orden", "falta"
        };

        public LiveCoachRoutingResult Route(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.ConversationalQuestion,
                    false);
            }

            var normalized = question.Trim().ToLowerInvariant();

            // 1. Check mutation request
            foreach (var kw in MutationKeywords)
            {
                if (ContainsWord(normalized, kw))
                {
                    return new LiveCoachRoutingResult(
                        LiveCoachIntentKind.MutationRequest,
                        false,
                        "Por el momento solo puedo responder consultas sobre tu sesión. El registro de series y cambios se realiza desde los controles de la pantalla.");
                }
            }

            // 2. Check out of scope
            foreach (var kw in OutOfScopeKeywords)
            {
                if (ContainsWord(normalized, kw))
                {
                    return new LiveCoachRoutingResult(
                        LiveCoachIntentKind.OutOfScope,
                        false,
                        "Solo puedo ayudarte con dudas sobre tu sesión activa de entrenamiento. Para temas médicos, dolores o nutrición, consultá a un profesional.");
                }
            }

            // 3. Check data query
            var requiresPreviousPerformance = false;
            foreach (var kw in HistoryKeywords)
            {
                if (normalized.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    requiresPreviousPerformance = true;
                    break;
                }
            }

            foreach (var kw in DataQueryKeywords)
            {
                if (normalized.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    return new LiveCoachRoutingResult(
                        LiveCoachIntentKind.DataQuery,
                        requiresPreviousPerformance);
                }
            }

            if (requiresPreviousPerformance)
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.DataQuery,
                    true);
            }

            // 4. Default to conversational question
            return new LiveCoachRoutingResult(
                LiveCoachIntentKind.ConversationalQuestion,
                false);
        }

        private static bool ContainsWord(string text, string word)
        {
            var index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return false;

            var beforeOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            var afterIndex = index + word.Length;
            var afterOk = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);

            return beforeOk && afterOk;
        }
    }
}
