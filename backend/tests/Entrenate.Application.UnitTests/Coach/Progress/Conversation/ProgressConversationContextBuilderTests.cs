using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Conversation;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.Progress.Conversation
{
    public class ProgressConversationContextBuilderTests
    {
        private static readonly Guid ExerciseId = Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

        [Fact]
        public void Build_ProjectsOnlyRelevantStructuredContext()
        {
            var latest = CreateLatestExposure();
            var analysis = new ProgressAnalysis(
                ExerciseId,
                3,
                3,
                [latest],
                latest,
                [latest],
                1,
                true,
                false);
            var recommendation = new ProgressRecommendation(
                ExerciseId,
                ProgressDecision.ReviewPerformance,
                EvidenceLevel.Medium,
                [ReasonCode.PrescriptionChanged]);
            var explanation = new ProgressExplanation(
                "Detección.",
                "Recomendación.",
                "Motivo.");

            var context = ProgressConversationContextBuilder.Build(
                analysis,
                recommendation,
                explanation);

            Assert.Equal(ExerciseId, context.ExerciseId);
            Assert.Equal(3, context.ValidExposureCount);
            Assert.Equal(3, context.DistinctSessionCount);
            Assert.Equal(1, context.ComparableExposureCount);
            Assert.True(context.PrescriptionChanged);
            Assert.False(context.LoadChanged);
            Assert.Equal(
                new ProgressConversationPrescription(3, 6, 10, 1, 2),
                context.CurrentPrescription);
            Assert.NotNull(context.LatestExposure);
            Assert.Equal(1, context.LatestExposure.BelowRangeSetCount);
            Assert.Equal(1, context.LatestExposure.InRangeSetCount);
            Assert.Equal(1, context.LatestExposure.TopRangeSetCount);
            Assert.Equal(
                ProgressDecision.ReviewPerformance,
                context.Decision);
            Assert.Equal(EvidenceLevel.Medium, context.EvidenceLevel);
            Assert.Equal(
                [ReasonCode.PrescriptionChanged],
                context.ReasonCodes);
            Assert.Same(explanation, context.Explanation);
        }

        [Fact]
        public void Build_RejectsRecommendationForAnotherExercise()
        {
            var analysis = new ProgressAnalysis(
                ExerciseId,
                0,
                0,
                [],
                null,
                [],
                0,
                false,
                false);
            var recommendation = new ProgressRecommendation(
                Guid.NewGuid(),
                ProgressDecision.InsufficientData,
                EvidenceLevel.Low,
                [ReasonCode.InsufficientHistory]);
            var explanation = new ProgressExplanation(
                "Detección.",
                "Recomendación.",
                "Motivo.");

            Assert.Throws<ArgumentException>(() =>
                ProgressConversationContextBuilder.Build(
                    analysis,
                    recommendation,
                    explanation));
        }

        private static ProgressExposureAnalysis CreateLatestExposure()
        {
            return new ProgressExposureAnalysis(
                Guid.NewGuid(),
                Guid.NewGuid(),
                ExerciseId,
                new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc),
                new ProgressPrescriptionSnapshot(3, 6, 10, 1, 2, 120),
                ExposureCompleteness.Full,
                3,
                [
                    new ProgressEvaluatedSetAnalysis(
                        1,
                        100m,
                        5,
                        0,
                        RepClassification.BelowRange,
                        RirClassification.HarderThanTarget),
                    new ProgressEvaluatedSetAnalysis(
                        2,
                        100m,
                        8,
                        1,
                        RepClassification.InRange,
                        RirClassification.OnTarget),
                    new ProgressEvaluatedSetAnalysis(
                        3,
                        100m,
                        10,
                        3,
                        RepClassification.TopRange,
                        RirClassification.EasierThanTarget)
                ],
                [],
                [],
                1m,
                LoadClassification.StableLoad,
                100m,
                false,
                false,
                false,
                false);
        }
    }
}
