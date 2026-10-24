using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.Progress.Explanations
{
    public class ProgressExplanationGeneratorTests
    {
        private static readonly Guid ExerciseId = Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

        [Theory]
        [MemberData(nameof(ExplanationCases))]
        public void Generate_ExplainsEachDecision(
            ProgressRecommendation recommendation,
            ProgressAnalysis analysis,
            ProgressExplanation expected)
        {
            var explanation = ProgressExplanationGenerator.Generate(
                recommendation,
                analysis);

            Assert.Equal(expected, explanation);
        }

        public static TheoryData<
            ProgressRecommendation,
            ProgressAnalysis,
            ProgressExplanation> ExplanationCases => new()
        {
            {
                Recommendation(
                    ProgressDecision.InsufficientData,
                    EvidenceLevel.Low,
                    ReasonCode.InsufficientHistory),
                Analysis(),
                new ProgressExplanation(
                    "Todavía no hay suficientes exposiciones válidas para " +
                        "evaluar tu progreso.",
                    "Registrá más sesiones completas antes de ajustar la " +
                        "progresión.",
                    "Sin historial válido, Entrenate no puede comparar tu " +
                        "rendimiento reciente.")
            },
            {
                Recommendation(
                    ProgressDecision.Maintain,
                    EvidenceLevel.Low,
                    ReasonCode.TopRangeReached,
                    ReasonCode.StableLoad),
                Analysis(),
                new ProgressExplanation(
                    "Alcanzaste el extremo superior del rango en la " +
                        "exposición más reciente.",
                    "Mantené la carga y la prescripción actuales en la " +
                        "próxima sesión.",
                    "El resultado es favorable, pero todavía falta " +
                        "confirmarlo en otra sesión comparable.")
            },
            {
                Recommendation(
                    ProgressDecision.IncreaseRepetitions,
                    EvidenceLevel.Low,
                    ReasonCode.WithinRepRange,
                    ReasonCode.RirOnTarget,
                    ReasonCode.StableLoad),
                Analysis(),
                new ProgressExplanation(
                    "Completaste las series planificadas dentro del rango, " +
                        "pero todavía no alcanzaste su extremo superior en " +
                        "todas.",
                    "En la próxima sesión, intentá aumentar las " +
                        "repeticiones dentro del rango indicado y mantené " +
                        "la carga actual.",
                    "La carga fue estable y el esfuerzo se mantuvo dentro " +
                        "del RIR objetivo.")
            },
            {
                Recommendation(
                    ProgressDecision.IncreaseLoad,
                    EvidenceLevel.Medium,
                    ReasonCode.TopRangeConsistent,
                    ReasonCode.RirOnTarget,
                    ReasonCode.StableLoad),
                Analysis(),
                new ProgressExplanation(
                    "Alcanzaste de forma consistente el extremo superior " +
                        "del rango de repeticiones.",
                    "En la próxima sesión, aumentá la carga de forma " +
                        "conservadora.",
                    "El rendimiento se repitió con carga estable y " +
                        "esfuerzo dentro del RIR objetivo.")
            },
            {
                Recommendation(
                    ProgressDecision.ReduceLoad,
                    EvidenceLevel.Medium,
                    ReasonCode.BelowMinimumReps,
                    ReasonCode.RirTooLow,
                    ReasonCode.StableLoad),
                Analysis(),
                new ProgressExplanation(
                    "El rendimiento reciente mostró de forma repetida una " +
                        "exigencia mayor que la prevista.",
                    "En la próxima sesión, reducí la carga para volver al " +
                        "rango prescripto.",
                    "Se repitieron series por debajo del mínimo de " +
                        "repeticiones y con un RIR menor al objetivo.")
            },
            {
                Recommendation(
                    ProgressDecision.ReviewPerformance,
                    EvidenceLevel.Low,
                    ReasonCode.BelowMinimumReps,
                    ReasonCode.RecentPerformanceDrop),
                Analysis(latestIsOverloaded: true),
                new ProgressExplanation(
                    "La exposición más reciente quedó por debajo de tu " +
                        "rendimiento comparable.",
                    "Revisá el rendimiento antes de modificar la carga o " +
                        "las repeticiones.",
                    "Una caída aislada no alcanza para reducir la carga " +
                        "automáticamente.")
            }
        };

        private static ProgressRecommendation Recommendation(
            ProgressDecision decision,
            EvidenceLevel evidenceLevel,
            params ReasonCode[] reasonCodes)
        {
            return new ProgressRecommendation(
                ExerciseId,
                decision,
                evidenceLevel,
                reasonCodes);
        }

        private static ProgressAnalysis Analysis(
            bool latestIsOverloaded = false)
        {
            ProgressExposureAnalysis? latest = latestIsOverloaded
                ? CreateOverloadedExposure()
                : null;

            return new ProgressAnalysis(
                ExerciseId,
                latest is null ? 0 : 1,
                latest is null ? 0 : 1,
                latest is null ? [] : [latest],
                latest,
                [],
                0,
                false,
                false);
        }

        private static ProgressExposureAnalysis CreateOverloadedExposure()
        {
            return new ProgressExposureAnalysis(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ExerciseId,
                new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc),
                new ProgressPrescriptionSnapshot(2, 6, 10, 1, 2, 120),
                ExposureCompleteness.Full,
                2,
                [],
                [],
                [],
                1m,
                LoadClassification.StableLoad,
                100m,
                true,
                true,
                false,
                true);
        }
    }
}
