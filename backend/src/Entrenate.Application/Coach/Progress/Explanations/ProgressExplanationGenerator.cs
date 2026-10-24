using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Recommendations;

namespace Entrenate.Application.Coach.Progress.Explanations
{
    public static class ProgressExplanationGenerator
    {
        public static ProgressExplanation Generate(
            ProgressRecommendation recommendation,
            ProgressAnalysis analysis)
        {
            ArgumentNullException.ThrowIfNull(recommendation);
            ArgumentNullException.ThrowIfNull(analysis);

            return recommendation.Decision switch
            {
                ProgressDecision.InsufficientData =>
                    ExplainInsufficientData(recommendation),
                ProgressDecision.Maintain =>
                    ExplainMaintain(recommendation),
                ProgressDecision.IncreaseRepetitions =>
                    ExplainIncreaseRepetitions(recommendation),
                ProgressDecision.IncreaseLoad =>
                    ExplainIncreaseLoad(recommendation),
                ProgressDecision.ReduceLoad =>
                    ExplainReduceLoad(recommendation),
                ProgressDecision.ReviewPerformance =>
                    ExplainReviewPerformance(recommendation, analysis),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(recommendation),
                    recommendation.Decision,
                    "La decisión de progreso no es válida.")
            };
        }

        private static ProgressExplanation ExplainInsufficientData(
            ProgressRecommendation recommendation)
        {
            var rationale = HasReason(
                recommendation,
                ReasonCode.PartialExposure)
                    ? "La única exposición disponible está incompleta."
                    : "Sin historial válido, Entrenate no puede comparar " +
                        "tu rendimiento reciente.";

            return new ProgressExplanation(
                "Todavía no hay suficientes exposiciones válidas para " +
                    "evaluar tu progreso.",
                "Registrá más sesiones completas antes de ajustar la " +
                    "progresión.",
                rationale);
        }

        private static ProgressExplanation ExplainMaintain(
            ProgressRecommendation recommendation)
        {
            var detection = HasReason(
                recommendation,
                ReasonCode.TopRangeReached)
                    ? "Alcanzaste el extremo superior del rango en la " +
                        "exposición más reciente."
                    : HasReason(recommendation, ReasonCode.PartialExposure)
                        ? "La exposición más reciente quedó incompleta."
                        : "Tu rendimiento reciente no muestra una señal " +
                            "clara para cambiar la progresión.";

            var rationale = HasReason(
                recommendation,
                ReasonCode.PrescriptionChanged)
                    ? "La prescripción cambió y todavía falta evidencia " +
                        "comparable en el nuevo contexto."
                    : HasReason(recommendation, ReasonCode.LoadChanged)
                        ? "La carga cambió y todavía falta evidencia " +
                            "comparable en el nuevo contexto."
                        : HasReason(
                            recommendation,
                            ReasonCode.TopRangeReached)
                                ? "El resultado es favorable, pero todavía " +
                                    "falta confirmarlo en otra sesión " +
                                    "comparable."
                                : "La evidencia disponible no justifica " +
                                    "aumentar ni reducir la carga.";

            return new ProgressExplanation(
                detection,
                "Mantené la carga y la prescripción actuales en la " +
                    "próxima sesión.",
                rationale);
        }

        private static ProgressExplanation ExplainIncreaseRepetitions(
            ProgressRecommendation recommendation)
        {
            var rationale = HasReason(
                    recommendation,
                    ReasonCode.StableLoad) &&
                HasReason(recommendation, ReasonCode.RirOnTarget)
                    ? "La carga fue estable y el esfuerzo se mantuvo " +
                        "dentro del RIR objetivo."
                    : "Cumpliste el mínimo de repeticiones y todavía hay " +
                        "margen dentro del rango prescripto.";

            return new ProgressExplanation(
                "Completaste las series planificadas dentro del rango, " +
                    "pero todavía no alcanzaste su extremo superior en " +
                    "todas.",
                "En la próxima sesión, intentá aumentar las repeticiones " +
                    "dentro del rango indicado y mantené la carga actual.",
                rationale);
        }

        private static ProgressExplanation ExplainIncreaseLoad(
            ProgressRecommendation recommendation)
        {
            string rationale;

            if (HasReason(recommendation, ReasonCode.RirMissing))
            {
                rationale = "El patrón se repitió en exposiciones " +
                    "comparables, aunque faltan registros de RIR.";
            }
            else if (recommendation.EvidenceLevel == EvidenceLevel.High)
            {
                rationale = "El patrón se sostuvo en al menos tres " +
                    "sesiones comparables, con carga estable y sin " +
                    "exceder el esfuerzo objetivo.";
            }
            else if (HasReason(recommendation, ReasonCode.RirOnTarget))
            {
                rationale = "El rendimiento se repitió con carga estable " +
                    "y esfuerzo dentro del RIR objetivo.";
            }
            else
            {
                rationale = "El patrón se repitió en exposiciones " +
                    "comparables con la misma carga y prescripción.";
            }

            return new ProgressExplanation(
                "Alcanzaste de forma consistente el extremo superior del " +
                    "rango de repeticiones.",
                "En la próxima sesión, aumentá la carga de forma " +
                    "conservadora.",
                rationale);
        }

        private static ProgressExplanation ExplainReduceLoad(
            ProgressRecommendation recommendation)
        {
            var belowMinimum = HasReason(
                recommendation,
                ReasonCode.BelowMinimumReps);
            var rirTooLow = HasReason(
                recommendation,
                ReasonCode.RirTooLow);

            var rationale = belowMinimum && rirTooLow
                ? "Se repitieron series por debajo del mínimo de " +
                    "repeticiones y con un RIR menor al objetivo."
                : rirTooLow
                    ? "El RIR quedó repetidamente por debajo del objetivo " +
                        "en exposiciones comparables."
                    : belowMinimum
                        ? "Las exposiciones comparables repitieron series " +
                            "por debajo del mínimo de repeticiones."
                        : "El patrón de sobrecarga se repitió en " +
                            "exposiciones comparables.";

            return new ProgressExplanation(
                "El rendimiento reciente mostró de forma repetida una " +
                    "exigencia mayor que la prevista.",
                "En la próxima sesión, reducí la carga para volver al " +
                    "rango prescripto.",
                rationale);
        }

        private static ProgressExplanation ExplainReviewPerformance(
            ProgressRecommendation recommendation,
            ProgressAnalysis analysis)
        {
            if (HasReason(recommendation, ReasonCode.ZeroExternalLoad))
            {
                return new ProgressExplanation(
                    "Dominaste repetidamente el extremo superior en un " +
                        "ejercicio sin carga externa registrada.",
                    "Revisá cómo progresar este ejercicio antes de cambiar " +
                        "la prescripción.",
                    "El modelo actual no puede traducir ese rendimiento " +
                        "en un aumento de carga registrado.");
            }

            if (HasReason(
                recommendation,
                ReasonCode.RepeatedPartialExposure))
            {
                return new ProgressExplanation(
                    "Las últimas exposiciones de sesiones distintas " +
                        "quedaron incompletas.",
                    "Revisá el rendimiento antes de modificar la carga o " +
                        "las repeticiones.",
                    "Las exposiciones parciales repetidas no permiten " +
                        "indicar una progresión directa.");
            }

            if (HasReason(recommendation, ReasonCode.MixedLoad))
            {
                return new ProgressExplanation(
                    "Las cargas variaron dentro de las exposiciones " +
                        "recientes.",
                    "Revisá el rendimiento antes de modificar la carga o " +
                        "las repeticiones.",
                    "Sin una carga estable de referencia, las exposiciones " +
                        "no son comparables para ajustar la carga.");
            }

            if (HasReason(
                recommendation,
                ReasonCode.RecentPerformanceDrop))
            {
                var latestIsOverloaded = analysis.LatestExposure?
                    .IsOverloaded == true;

                return new ProgressExplanation(
                    latestIsOverloaded
                        ? "La exposición más reciente quedó por debajo de " +
                            "tu rendimiento comparable."
                        : "Las exposiciones recientes muestran resultados " +
                            "contrapuestos.",
                    "Revisá el rendimiento antes de modificar la carga o " +
                        "las repeticiones.",
                    latestIsOverloaded
                        ? "Una caída aislada no alcanza para reducir la " +
                            "carga automáticamente."
                        : "Los resultados opuestos no permiten definir una " +
                            "progresión directa.");
            }

            return new ProgressExplanation(
                "La evidencia reciente requiere una revisión antes de " +
                    "progresar.",
                "Revisá el rendimiento antes de modificar la carga o las " +
                    "repeticiones.",
                "La información disponible muestra una situación " +
                    "relevante, pero no una dirección de progresión clara.");
        }

        private static bool HasReason(
            ProgressRecommendation recommendation,
            ReasonCode reason)
        {
            return recommendation.ReasonCodes.Contains(reason);
        }
    }
}
