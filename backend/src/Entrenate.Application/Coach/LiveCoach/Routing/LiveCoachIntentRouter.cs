using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Entrenate.Application.Coach.LiveCoach.Routing
{
    public enum LiveCoachIntentKind
    {
        ConversationalQuestion,
        DataQuery,
        RegisterCurrentSet,
        SkipCurrentExercise,
        ReactivateExercise,
        ConfirmPendingAction,
        CancelPendingAction,
        UpdatePendingCurrentSet,
        PendingActionClarification,
        PendingActionUnavailable,
        UnsupportedRestDurationMutation,
        StartRestTimer,
        StopRestTimer,
        RestTimerQuery,
        PreviousSetPerformance,
        PreviousSetRir,
        RemainingSets,
        BestSetToday,
        PreviousExercisePerformance,
        CompareWithPreviousSession,
        NextExercise,
        RemainingExercises,
        SessionProgress,
        OtherMutation,
        OutOfScope
    }

    public sealed record LiveCoachRoutingResult(
        LiveCoachIntentKind Kind,
        bool RequiresPreviousPerformance,
        string? DirectAnswer = null);

    public interface ILiveCoachIntentRouter
    {
        LiveCoachRoutingResult Route(string question, bool hasPendingAction = false);
    }

    public sealed class LiveCoachIntentRouter : ILiveCoachIntentRouter
    {
        private static readonly HashSet<string> ConfirmPendingActionPhrases =
            new(StringComparer.Ordinal)
            {
                "si", "dale", "registrala", "registralo", "confirma", "confirmo", "hacelo"
            };

        private static readonly HashSet<string> CancelPendingActionPhrases =
            new(StringComparer.Ordinal)
            {
                "no", "cancela", "dejalo", "no la registres", "olvidalo"
            };

        private static readonly HashSet<string> PendingSetCorrectionWords = new(StringComparer.Ordinal)
        {
            "fueron", "eran", "hice", "pone", "cambia", "cambiame", "corrige", "corregi"
        };

        private static readonly HashSet<string> PendingSetFieldWords = new(StringComparer.Ordinal)
        {
            "rep", "reps", "repeticion", "repeticiones", "kg", "kilo", "kilos", "peso", "rir"
        };

        private static readonly HashSet<string> NumberWords = new(StringComparer.Ordinal)
        {
            "cero", "un", "uno", "una", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
            "diez", "once", "doce", "trece", "catorce", "quince", "dieciseis", "diecisiete", "dieciocho",
            "diecinueve", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta",
            "noventa", "cien", "ciento", "doscientos"
        };

        private static readonly HashSet<string> RestTargetWords = new(StringComparer.Ordinal)
        {
            "descanso", "timer", "temporizador", "reloj", "cronometro"
        };

        private static readonly HashSet<string> StartRestTimerVerbs = new(StringComparer.Ordinal)
        {
            "inicia", "inicias", "inicie", "inicies", "iniciar", "iniciame", "iniciate", "iniciemos", "iniciarlo", "iniciarla",
            "pone", "pones", "ponga", "pongas", "poner", "poneme", "pongamos", "ponerlo", "ponerle",
            "arranca", "arrancas", "arranque", "arranques", "arrancar", "arrancame", "arranquemos", "arrancarlo",
            "empeza", "empezas", "empieza", "empiezas", "empiece", "empieces", "empezar", "empezame", "empecemos", "empezarlo",
            "comenza", "comenzas", "comienza", "comienzas", "comience", "comiences", "comenzar", "comenzame", "comencemos", "comenzarlo",
            "activa", "activas", "active", "actives", "activar", "activame", "activemos", "activarlo",
            "larga", "largas", "largue", "largues", "largar", "largame", "larguemos", "largarlo",
            "corre", "corres", "corra", "corras", "correr", "correme", "corramos", "correrlo",
            "conta", "contas", "cuenta", "cuentas", "cuente", "cuentes", "contar", "contame", "contemos", "contarlo",
            "temporiza", "temporizas", "temporice", "temporices", "temporizar", "temporicemos", "temporizalo",
            "dale", "mandale", "metele"
        };

        private static readonly HashSet<string> StopRestTimerVerbs = new(StringComparer.Ordinal)
        {
            "para", "paras", "pare", "pares", "parar", "parame", "paremos", "paralo",
            "detene", "detenes", "detenga", "detengas", "detener", "deteneme", "detengamos", "detenelo",
            "cancela", "cancelas", "cancele", "canceles", "cancelar", "cancelame", "cancelemos", "cancelalo",
            "saca", "sacas", "saque", "saques", "sacar", "sacame", "saquemos", "sacalo",
            "frena", "frenas", "frene", "frenes", "frenar", "frename", "frenemos", "frenalo",
            "corta", "cortas", "corte", "cortes", "cortar", "cortame", "cortemos", "cortalo",
            "termina", "terminas", "termine", "termines", "terminar", "terminame", "terminemos", "terminalo",
            "finaliza", "finalizas", "finalice", "finalices", "finalizar", "finalizame", "finalicemos", "finalizalo",
            "apaga", "apagas", "apague", "apagues", "apagar", "apagame", "apaguemos", "apagalo"
        };

        private static readonly HashSet<string> StandaloneRestQueryPhrases = new(StringComparer.Ordinal)
        {
            "ya termino", "ya termino el descanso", "termino el descanso",
            "cuanto falta", "cuanto falta para que termine"
        };

        private static readonly string[] RestQueryIndicators =
        {
            "cuanto", "cuantos", "cuanta", "cuantas",
            "cual", "cuales",
            "que descanso", "que timer", "que tiempo",
            "como vengo", "como voy",
            "falta", "queda", "quedan", "restante", "restantes",
            "dura", "duracion",
            "toca",
            "debo descansar", "tengo que descansar", "hay que descansar",
            "esta activo", "esta corriendo",
            "necesito", "quiero"
        };

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

        public LiveCoachRoutingResult Route(string question, bool hasPendingAction = false)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.ConversationalQuestion,
                    false);
            }

            var normalized = question.Trim().ToLowerInvariant();
            var pendingDecision = NormalizePendingDecision(question);
            var confirmsPending = ConfirmPendingActionPhrases.Contains(pendingDecision);
            var cancelsPending = CancelPendingActionPhrases.Contains(pendingDecision);

            if (hasPendingAction)
            {
                if (confirmsPending)
                    return new LiveCoachRoutingResult(LiveCoachIntentKind.ConfirmPendingAction, false);

                if (cancelsPending)
                    return new LiveCoachRoutingResult(LiveCoachIntentKind.CancelPendingAction, false);

                if (IsPendingSetUpdateRequest(pendingDecision))
                    return new LiveCoachRoutingResult(LiveCoachIntentKind.UpdatePendingCurrentSet, false);

                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.PendingActionClarification,
                    false,
                    "¿Querés que registre la serie? Decime sí o no.");
            }

            if (confirmsPending || cancelsPending)
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.PendingActionUnavailable,
                    false,
                    "No hay una serie pendiente para confirmar.");
            }

            if (IsCustomRestDurationRequest(pendingDecision))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.UnsupportedRestDurationMutation,
                    false,
                    "Por ahora uso el tiempo de descanso indicado por la rutina.");
            }

            if (IsStopRestTimerRequest(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.StopRestTimer, false);

            if (IsStartRestTimerRequest(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.StartRestTimer, false);

            if (IsRestQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.RestTimerQuery, false);

            if (IsReactivateExerciseRequest(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.ReactivateExercise, false);

            if (IsSkipCurrentExerciseRequest(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.SkipCurrentExercise, false);

            if (IsPendingSetUpdateRequest(pendingDecision) &&
                !LooksLikeCompleteSetStatement(pendingDecision) &&
                !pendingDecision.StartsWith("hice ", StringComparison.Ordinal))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.PendingActionUnavailable,
                    false,
                    "No hay una propuesta de serie pendiente para editar.");
            }

            if (IsRegisterCurrentSetRequest(pendingDecision))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.RegisterCurrentSet,
                    false);
            }

            if (IsPendingSetUpdateRequest(pendingDecision))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.PendingActionUnavailable,
                    false,
                    "No hay una propuesta de serie pendiente para editar.");
            }

            if (IsBareNumericSequence(pendingDecision))
            {
                return new LiveCoachRoutingResult(
                    LiveCoachIntentKind.OtherMutation,
                    false,
                    "Indicame cuál valor corresponde al peso, las repeticiones y el RIR.");
            }

            // 1. Check performance queries (deterministic read-only)
            if (IsBestSetQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.BestSetToday, false);

            if (IsCompareWithPreviousSessionQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.CompareWithPreviousSession, false);

            if (IsPreviousSetRirQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.PreviousSetRir, false);

            if (IsPreviousExercisePerformanceQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.PreviousExercisePerformance, false);

            if (IsPreviousSetPerformanceQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.PreviousSetPerformance, false);

            if (IsNextExerciseQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.NextExercise, false);

            if (IsRemainingExercisesQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.RemainingExercises, false);

            if (IsRemainingSetsQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.RemainingSets, false);

            if (IsSessionProgressQuery(pendingDecision))
                return new LiveCoachRoutingResult(LiveCoachIntentKind.SessionProgress, false);

            // 2. Check mutation request
            foreach (var kw in MutationKeywords)
            {
                if (ContainsWord(normalized, kw))
                {
                    return new LiveCoachRoutingResult(
                        LiveCoachIntentKind.OtherMutation,
                        false,
                        "Por ahora solo puedo registrar la serie actual.");
                }
            }

            // 3. Check out of scope
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

            // 4. Check data query
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

        private static bool IsRegisterCurrentSetRequest(string text)
        {
            if (text.StartsWith("cuanto ", StringComparison.Ordinal) ||
                text.StartsWith("cuantos ", StringComparison.Ordinal) ||
                text.StartsWith("cuantas ", StringComparison.Ordinal) ||
                text.StartsWith("que ", StringComparison.Ordinal) ||
                text.StartsWith("cual ", StringComparison.Ordinal) ||
                text.StartsWith("como ", StringComparison.Ordinal) ||
                text.Contains("anterior", StringComparison.Ordinal) ||
                text.Contains("ultima vez", StringComparison.Ordinal) ||
                text.Contains("vez pasada", StringComparison.Ordinal))
            {
                return false;
            }

            var hasRegisterVerb =
                text.Contains("hice", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("fueron", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("termine", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("completa", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("completame", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("anota", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("anotame", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("registra", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("registrame", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("carga", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("cargame", StringComparison.OrdinalIgnoreCase);

            var hasSetData =
                text.Contains("rep", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("kilo", StringComparison.OrdinalIgnoreCase) ||
                text.Contains(" kg", StringComparison.OrdinalIgnoreCase) ||
                text.Contains(" con ", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("rir", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("sin peso", StringComparison.OrdinalIgnoreCase);

            var targetsCurrentSet = text.Contains("serie", StringComparison.OrdinalIgnoreCase);

            return hasRegisterVerb && (hasSetData || targetsCurrentSet);
        }

        private static bool IsSkipCurrentExerciseRequest(string text)
        {
            return text.Contains("salta este ejercicio", StringComparison.Ordinal) ||
                   text.Contains("saltemos este ejercicio", StringComparison.Ordinal) ||
                   text.Contains("omiti este ejercicio", StringComparison.Ordinal) ||
                   text.Contains("pasa al siguiente ejercicio", StringComparison.Ordinal) ||
                   text.Contains("no quiero hacer este ejercicio", StringComparison.Ordinal);
        }

        private static bool IsReactivateExerciseRequest(string text)
        {
            return text.StartsWith("reactiva ", StringComparison.Ordinal) ||
                   text.StartsWith("volve a activar ", StringComparison.Ordinal) ||
                   text.StartsWith("quiero volver a hacer ", StringComparison.Ordinal) ||
                   text.StartsWith("recupera ", StringComparison.Ordinal) ||
                   (text.StartsWith("saca ", StringComparison.Ordinal) &&
                    text.Contains(" de omitidos", StringComparison.Ordinal)) ||
                   text.StartsWith("volvamos a ", StringComparison.Ordinal);
        }

        private static bool IsBestSetQuery(string text)
        {
            return text.Contains("mejor serie", StringComparison.Ordinal);
        }

        private static bool IsCompareWithPreviousSessionQuery(string text)
        {
            var hasComparisonTarget = text.Contains("sesion anterior", StringComparison.Ordinal) ||
                                      text.Contains("vez pasada", StringComparison.Ordinal) ||
                                      text.Contains("sesion previa", StringComparison.Ordinal) ||
                                      text.Contains("ultima vez", StringComparison.Ordinal);

            var hasComparisonCue = text.Contains("mas que", StringComparison.Ordinal) ||
                                   text.Contains("mejor que", StringComparison.Ordinal) ||
                                   text.Contains("comparado", StringComparison.Ordinal) ||
                                   text.Contains("respecto a", StringComparison.Ordinal) ||
                                   text.Contains("levantando mas", StringComparison.Ordinal);

            return hasComparisonTarget && hasComparisonCue;
        }

        private static bool IsPreviousSetRirQuery(string text)
        {
            if (!text.Contains("rir", StringComparison.Ordinal))
                return false;

            if (LooksLikeCompleteSetStatement(text))
                return false;

            var hasRirQuestion = text.Contains("que rir", StringComparison.Ordinal) ||
                                 text.Contains("cuanto rir", StringComparison.Ordinal) ||
                                 text.Contains("con que rir", StringComparison.Ordinal);

            if (hasRirQuestion)
                return true;

            var targetsPrevious = text.Contains("anterior", StringComparison.Ordinal) ||
                                  text.Contains("ultima", StringComparison.Ordinal) ||
                                  text.Contains("recien", StringComparison.Ordinal);

            var asksForRir = text.Contains("cuanto", StringComparison.Ordinal) ||
                             text.Contains("que", StringComparison.Ordinal) ||
                             text.Contains("cual", StringComparison.Ordinal) ||
                             text.Contains("como", StringComparison.Ordinal);

            return targetsPrevious && asksForRir;
        }

        private static bool IsPreviousExercisePerformanceQuery(string text)
        {
            if (LooksLikeCompleteSetStatement(text))
                return false;

            if (IsCompareWithPreviousSessionQuery(text))
                return false;

            var hasPreviousSessionCue = text.Contains("ultima vez", StringComparison.Ordinal) ||
                                        text.Contains("vez pasada", StringComparison.Ordinal) ||
                                        text.Contains("sesion anterior", StringComparison.Ordinal) ||
                                        text.Contains("sesion previa", StringComparison.Ordinal);

            if (!hasPreviousSessionCue)
                return false;

            return text.Contains("ejercicio", StringComparison.Ordinal) ||
                   text.Contains("con cuanto", StringComparison.Ordinal) ||
                   text.Contains("que hice", StringComparison.Ordinal) ||
                   text.Contains("cuanto levante", StringComparison.Ordinal) ||
                   text.Contains("que peso", StringComparison.Ordinal) ||
                   text.Contains("como me fue", StringComparison.Ordinal) ||
                   text.Contains("cuanto use", StringComparison.Ordinal);
        }

        private static bool IsPreviousSetPerformanceQuery(string text)
        {
            if (text.Contains("rir", StringComparison.Ordinal))
                return false;

            if (LooksLikeCompleteSetStatement(text))
                return false;

            if (IsPreviousExercisePerformanceQuery(text))
                return false;

            var targetsPrevious = text.Contains("serie anterior", StringComparison.Ordinal) ||
                                  text.Contains("ultima serie", StringComparison.Ordinal) ||
                                  text.Contains("la anterior", StringComparison.Ordinal) ||
                                  text.Contains("en la anterior", StringComparison.Ordinal) ||
                                  text.Contains("la ultima", StringComparison.Ordinal) ||
                                  text.Contains("recien", StringComparison.Ordinal);

            if (!targetsPrevious)
                return false;

            return text.Contains("cuanto", StringComparison.Ordinal) ||
                   text.Contains("que hice", StringComparison.Ordinal) ||
                   text.Contains("como salio", StringComparison.Ordinal) ||
                   text.Contains("como fue", StringComparison.Ordinal) ||
                   text.Contains("que peso", StringComparison.Ordinal) ||
                   text.Contains("cuanto levante", StringComparison.Ordinal) ||
                   text.Contains("que registre", StringComparison.Ordinal);
        }

        private static bool IsNextExerciseQuery(string text)
        {
            if (!text.Contains("ejercicio", StringComparison.Ordinal))
                return false;

            if (text.Contains("cambia", StringComparison.Ordinal) ||
                text.Contains("modifica", StringComparison.Ordinal) ||
                text.Contains("borra", StringComparison.Ordinal) ||
                text.Contains("saca", StringComparison.Ordinal) ||
                text.Contains("elimina", StringComparison.Ordinal) ||
                text.Contains("pone", StringComparison.Ordinal) ||
                text.Contains("anota", StringComparison.Ordinal))
            {
                return false;
            }

            return text.Contains("sigue", StringComparison.Ordinal) ||
                   text.Contains("viene", StringComparison.Ordinal) ||
                   text.Contains("siguiente", StringComparison.Ordinal) ||
                   text.Contains("proximo", StringComparison.Ordinal) ||
                   text.Contains("despues de este", StringComparison.Ordinal) ||
                   (text.Contains("toca", StringComparison.Ordinal) && !text.Contains("descanso", StringComparison.Ordinal));
        }

        private static bool IsRemainingExercisesQuery(string text)
        {
            if (!text.Contains("ejercicio", StringComparison.Ordinal))
                return false;

            if (IsNextExerciseQuery(text))
                return false;

            var hasRemainingCue = text.Contains("falta", StringComparison.Ordinal) ||
                                  text.Contains("faltan", StringComparison.Ordinal) ||
                                  text.Contains("queda", StringComparison.Ordinal) ||
                                  text.Contains("quedan", StringComparison.Ordinal) ||
                                  text.Contains("restan", StringComparison.Ordinal);

            return hasRemainingCue;
        }

        private static bool IsRemainingSetsQuery(string text)
        {
            if (!text.Contains("serie", StringComparison.Ordinal))
                return false;

            var hasRemainingCue = text.Contains("falta", StringComparison.Ordinal) ||
                                  text.Contains("faltan", StringComparison.Ordinal) ||
                                  text.Contains("queda", StringComparison.Ordinal) ||
                                  text.Contains("quedan", StringComparison.Ordinal) ||
                                  text.Contains("restan", StringComparison.Ordinal);

            return hasRemainingCue;
        }

        private static bool IsSessionProgressQuery(string text)
        {
            var hasProgressTarget = text.Contains("para terminar", StringComparison.Ordinal) ||
                                    text.Contains("para finalizar", StringComparison.Ordinal) ||
                                    text.Contains("terminar la sesion", StringComparison.Ordinal) ||
                                    text.Contains("terminar el entrenamiento", StringComparison.Ordinal);

            if (!hasProgressTarget)
                return false;

            return text.Contains("cuanto", StringComparison.Ordinal) ||
                   text.Contains("falta", StringComparison.Ordinal) ||
                   text.Contains("queda", StringComparison.Ordinal);
        }

        private static bool IsBareNumericSequence(string normalizedText)
        {
            var tokens = Tokenize(normalizedText);
            return tokens.Count >= 2 && tokens.All(token =>
                decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out _));
        }

        private static bool LooksLikeCompleteSetStatement(string normalizedText)
        {
            var tokens = Tokenize(normalizedText);
            var hasWeight = normalizedText.Contains(" con ", StringComparison.Ordinal) ||
                            tokens.Exists(token => token is "kg" or "kilo" or "kilos" or "peso");
            var hasRepetitions = tokens.Exists(token =>
                token is "rep" or "reps" or "repeticion" or "repeticiones") ||
                tokens.Exists(token => token is "hice" or "fueron");

            return hasWeight && hasRepetitions;
        }

        private static bool IsPendingSetUpdateRequest(string normalizedText)
        {
            var tokens = Tokenize(normalizedText);
            var hasNumber = tokens.Exists(token =>
                decimal.TryParse(token.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out _) ||
                NumberWords.Contains(token));
            if (!hasNumber)
                return false;

            var hasField = tokens.Exists(PendingSetFieldWords.Contains);
            var hasCorrectionCue = normalizedText.Contains("en realidad", StringComparison.Ordinal) ||
                                   tokens.Exists(PendingSetCorrectionWords.Contains);

            return hasField || hasCorrectionCue;
        }

        private static bool IsCustomRestDurationRequest(string normalizedText)
        {
            var tokens = Tokenize(normalizedText);
            var hasTarget = tokens.Exists(RestTargetWords.Contains);
            var adjustsDuration = tokens.Exists(token =>
                token is "sumale" or "restale" or "agrega" or "agregale");
            var hasTimeUnit = tokens.Exists(token =>
                token is "minuto" or "minutos" or "segundo" or "segundos");
            var hasNumber = tokens.Exists(token => int.TryParse(token, out _));
            var hasTimerStartVerb = tokens.Exists(StartRestTimerVerbs.Contains);

            return (adjustsDuration && hasNumber) ||
                   (hasTimeUnit && hasNumber && (hasTarget || hasTimerStartVerb));
        }

        private static bool IsRestQuery(string normalizedText)
        {
            if (StandaloneRestQueryPhrases.Contains(normalizedText))
                return true;

            var tokens = Tokenize(normalizedText);
            if (!tokens.Exists(RestTargetWords.Contains))
                return false;

            foreach (var indicator in RestQueryIndicators)
            {
                if (normalizedText.Contains(indicator, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsStartRestTimerRequest(string normalizedText)
        {
            var tokens = Tokenize(normalizedText);
            return tokens.Exists(RestTargetWords.Contains) &&
                   tokens.Exists(StartRestTimerVerbs.Contains);
        }

        private static bool IsStopRestTimerRequest(string normalizedText)
        {
            var tokens = Tokenize(normalizedText);
            return tokens.Exists(RestTargetWords.Contains) &&
                   tokens.Exists(StopRestTimerVerbs.Contains);
        }

        private static List<string> Tokenize(string normalizedText)
            => new(normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        private static string NormalizePendingDecision(string text)
        {
            var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(character) || char.IsWhiteSpace(character))
                    builder.Append(character);
            }

            return string.Join(
                ' ',
                builder.ToString()
                    .Normalize(NormalizationForm.FormC)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
