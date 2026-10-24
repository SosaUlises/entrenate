using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Recommendations;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.Progress.Recommendations
{
    public class ProgressRecommendationEngineTests
    {
        private static readonly Guid ExerciseId = Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

        [Theory]
        [MemberData(nameof(DecisionCases))]
        public void Recommend_AppliesDeterministicDecisionRules(
            string _,
            IReadOnlyCollection<ProgressExposure> exposures,
            ProgressDecision expectedDecision,
            EvidenceLevel expectedEvidence,
            ReasonCode expectedReason)
        {
            var analysis = ProgressAnalyzer.Analyze(
                ExerciseId,
                exposures);

            var recommendation = ProgressRecommendationEngine.Recommend(
                analysis);

            Assert.Equal(expectedDecision, recommendation.Decision);
            Assert.Equal(expectedEvidence, recommendation.EvidenceLevel);
            Assert.Contains(expectedReason, recommendation.ReasonCodes);
            Assert.Equal(
                recommendation.ReasonCodes.OrderBy(reason => (int)reason),
                recommendation.ReasonCodes);
        }

        public static TheoryData<
            string,
            IReadOnlyCollection<ProgressExposure>,
            ProgressDecision,
            EvidenceLevel,
            ReasonCode> DecisionCases => new()
        {
            {
                "No history",
                Array.Empty<ProgressExposure>(),
                ProgressDecision.InsufficientData,
                EvidenceLevel.Low,
                ReasonCode.InsufficientHistory
            },
            {
                "Increase repetitions",
                new[]
                {
                    CreateExposure(1, 11, 100m, [8, 7], [2, 1])
                },
                ProgressDecision.IncreaseRepetitions,
                EvidenceLevel.Low,
                ReasonCode.WithinRepRange
            },
            {
                "Increase load with sufficient RIR",
                new[]
                {
                    CreateExposure(1, 11, 100m, [10, 10], [2, 1]),
                    CreateExposure(2, 12, 100m, [10, 10], [1, 2])
                },
                ProgressDecision.IncreaseLoad,
                EvidenceLevel.Medium,
                ReasonCode.TopRangeConsistent
            },
            {
                "Increase load with high evidence",
                new[]
                {
                    CreateExposure(1, 11, 100m, [10, 10], [2, 1]),
                    CreateExposure(2, 12, 100m, [10, 10], [1, 2]),
                    CreateExposure(3, 13, 100m, [10, 10], [2, 2])
                },
                ProgressDecision.IncreaseLoad,
                EvidenceLevel.High,
                ReasonCode.TopRangeConsistent
            },
            {
                "Increase load without sufficient RIR",
                new[]
                {
                    CreateExposure(1, 11, 100m, [10, 10], [null, null]),
                    CreateExposure(2, 12, 100m, [10, 10], [null, null]),
                    CreateExposure(3, 13, 100m, [10, 10], [null, null])
                },
                ProgressDecision.IncreaseLoad,
                EvidenceLevel.Medium,
                ReasonCode.RirMissing
            },
            {
                "Reduce load with sufficient RIR",
                new[]
                {
                    CreateExposure(1, 11, 100m, [5, 5], [0, 0]),
                    CreateExposure(2, 12, 100m, [5, 5], [0, 0])
                },
                ProgressDecision.ReduceLoad,
                EvidenceLevel.Medium,
                ReasonCode.RirTooLow
            },
            {
                "Reduce load without RIR has priority over review",
                new[]
                {
                    CreateExposure(1, 11, 100m, [5, 5], [null, null]),
                    CreateExposure(2, 12, 100m, [5, 5], [null, null]),
                    CreateExposure(3, 13, 100m, [5, 5], [null, null])
                },
                ProgressDecision.ReduceLoad,
                EvidenceLevel.Medium,
                ReasonCode.RirMissing
            },
            {
                "Isolated overloaded exposure",
                new[]
                {
                    CreateExposure(1, 11, 100m, [5, 5], [0, 0]),
                    CreateExposure(2, 12, 100m, [8, 8], [2, 2])
                },
                ProgressDecision.ReviewPerformance,
                EvidenceLevel.Low,
                ReasonCode.RecentPerformanceDrop
            },
            {
                "Repeated partial exposures",
                new[]
                {
                    CreateExposure(1, 11, 100m, [8], [2]),
                    CreateExposure(2, 12, 100m, [8], [2])
                },
                ProgressDecision.ReviewPerformance,
                EvidenceLevel.Medium,
                ReasonCode.RepeatedPartialExposure
            },
            {
                "First complete top-range exposure",
                new[]
                {
                    CreateExposure(1, 11, 100m, [10, 10], [2, 1])
                },
                ProgressDecision.Maintain,
                EvidenceLevel.Low,
                ReasonCode.TopRangeReached
            },
            {
                "Repeated zero-load mastery",
                new[]
                {
                    CreateExposure(1, 11, 0m, [10, 10], [2, 1]),
                    CreateExposure(2, 12, 0m, [10, 10], [2, 1])
                },
                ProgressDecision.ReviewPerformance,
                EvidenceLevel.Medium,
                ReasonCode.ZeroExternalLoad
            },
            {
                "Repeated mixed loads are reviewed without changing load",
                new[]
                {
                    CreateMixedExposure(1, 11, [100m, 95m]),
                    CreateMixedExposure(2, 12, [102.5m, 97.5m])
                },
                ProgressDecision.ReviewPerformance,
                EvidenceLevel.Medium,
                ReasonCode.MixedLoad
            },
            {
                "Same-session partials do not establish repetition",
                new[]
                {
                    CreateExposure(1, 11, 100m, [8], [2]),
                    CreateExposure(2, 11, 100m, [8], [2])
                },
                ProgressDecision.Maintain,
                EvidenceLevel.Low,
                ReasonCode.PartialExposure
            }
        };

        private static ProgressExposure CreateExposure(
            int exerciseSessionNumber,
            int sessionNumber,
            decimal weight,
            int[] repetitions,
            int?[] rir)
        {
            var sets = repetitions
                .Select((reps, index) => new ProgressCompletedSet(
                    index + 1,
                    weight,
                    reps,
                    rir[index]))
                .ToList();

            return new ProgressExposure(
                CreateGuid(exerciseSessionNumber),
                CreateGuid(sessionNumber),
                ExerciseId,
                new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc),
                new ProgressPrescriptionSnapshot(
                    2,
                    6,
                    10,
                    1,
                    2,
                    120),
                sets);
        }

        private static ProgressExposure CreateMixedExposure(
            int exerciseSessionNumber,
            int sessionNumber,
            decimal[] weights)
        {
            var sets = weights
                .Select((weight, index) => new ProgressCompletedSet(
                    index + 1,
                    weight,
                    10,
                    2))
                .ToList();

            return new ProgressExposure(
                CreateGuid(exerciseSessionNumber),
                CreateGuid(sessionNumber),
                ExerciseId,
                new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc),
                new ProgressPrescriptionSnapshot(
                    2,
                    6,
                    10,
                    1,
                    2,
                    120),
                sets);
        }

        private static Guid CreateGuid(int value)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
        }
    }
}
