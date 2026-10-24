using Entrenate.Application.Coach.Progress.Analysis;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.Progress.Analysis
{
    public class ProgressAnalyzerTests
    {
        private static readonly Guid ExerciseId = Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

        [Fact]
        public void Analyze_BuildsComparableSequenceAcrossDistinctSessions()
        {
            var latest = CreateExposure(
                exerciseSessionId: CreateGuid(1),
                sessionId: CreateGuid(11),
                weight: 100m,
                restSeconds: 120);
            var previous = CreateExposure(
                exerciseSessionId: CreateGuid(2),
                sessionId: CreateGuid(12),
                weight: 100m,
                restSeconds: 180);
            var oldest = CreateExposure(
                exerciseSessionId: CreateGuid(3),
                sessionId: CreateGuid(13),
                weight: 100m,
                restSeconds: 90);

            var result = ProgressAnalyzer.Analyze(
                ExerciseId,
                [latest, previous, oldest]);

            Assert.Equal(3, result.ValidExposureCount);
            Assert.Equal(3, result.DistinctSessionCount);
            Assert.Equal(latest.ExerciseSessionId,
                result.LatestExposure!.ExerciseSessionId);
            Assert.Equal(3, result.ComparableExposureCount);
            Assert.Equal(
                new[]
                {
                    latest.ExerciseSessionId,
                    previous.ExerciseSessionId,
                    oldest.ExerciseSessionId
                },
                result.ComparableExposures.Select(x => x.ExerciseSessionId));
            Assert.False(result.PrescriptionChanged);
            Assert.False(result.LoadChanged);
        }

        [Fact]
        public void Analyze_DetectsRelevantPrescriptionChange()
        {
            var latest = CreateExposure(
                exerciseSessionId: CreateGuid(1),
                sessionId: CreateGuid(11),
                weight: 100m,
                maxReps: 10);
            var previous = CreateExposure(
                exerciseSessionId: CreateGuid(2),
                sessionId: CreateGuid(12),
                weight: 100m,
                maxReps: 12);

            var result = ProgressAnalyzer.Analyze(
                ExerciseId,
                [latest, previous]);

            Assert.True(result.PrescriptionChanged);
            Assert.False(result.LoadChanged);
            Assert.Equal(1, result.ComparableExposureCount);
        }

        [Fact]
        public void Analyze_DetectsUnambiguousStableLoadChange()
        {
            var latest = CreateExposure(
                exerciseSessionId: CreateGuid(1),
                sessionId: CreateGuid(11),
                weight: 102.5m);
            var previous = CreateExposure(
                exerciseSessionId: CreateGuid(2),
                sessionId: CreateGuid(12),
                weight: 100m);

            var result = ProgressAnalyzer.Analyze(
                ExerciseId,
                [latest, previous]);

            Assert.False(result.PrescriptionChanged);
            Assert.True(result.LoadChanged);
            Assert.Equal(1, result.ComparableExposureCount);
        }

        [Fact]
        public void Analyze_PreservesSameSessionOccurrencesWithoutCountingThemAsDistinctEvidence()
        {
            var latest = CreateExposure(
                exerciseSessionId: CreateGuid(1),
                sessionId: CreateGuid(11),
                weight: 100m);
            var sameSessionOccurrence = CreateExposure(
                exerciseSessionId: CreateGuid(2),
                sessionId: latest.SessionId,
                weight: 100m);
            var previousSession = CreateExposure(
                exerciseSessionId: CreateGuid(3),
                sessionId: CreateGuid(12),
                weight: 100m);

            var result = ProgressAnalyzer.Analyze(
                ExerciseId,
                [latest, sameSessionOccurrence, previousSession]);

            Assert.Equal(3, result.ValidExposureCount);
            Assert.Equal(2, result.DistinctSessionCount);
            Assert.Equal(
                new[]
                {
                    latest.ExerciseSessionId,
                    sameSessionOccurrence.ExerciseSessionId,
                    previousSession.ExerciseSessionId
                },
                result.Exposures.Select(x => x.ExerciseSessionId));
            Assert.Equal(2, result.ComparableExposureCount);
            Assert.DoesNotContain(
                result.ComparableExposures,
                exposure =>
                    exposure.ExerciseSessionId ==
                        sameSessionOccurrence.ExerciseSessionId);
        }

        private static ProgressExposure CreateExposure(
            Guid exerciseSessionId,
            Guid sessionId,
            decimal weight,
            int maxReps = 10,
            int restSeconds = 120)
        {
            return new ProgressExposure(
                exerciseSessionId,
                sessionId,
                ExerciseId,
                new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc),
                new ProgressPrescriptionSnapshot(
                    2,
                    6,
                    maxReps,
                    1,
                    2,
                    restSeconds),
                [
                    new ProgressCompletedSet(1, weight, maxReps, 2),
                    new ProgressCompletedSet(2, weight, maxReps, 2)
                ]);
        }

        private static Guid CreateGuid(int value)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
        }
    }
}
