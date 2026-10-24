using Entrenate.Application.Coach.Progress.Analysis;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.Progress.Analysis
{
    public class ProgressExposureAnalyzerTests
    {
        [Fact]
        public void Analyze_PreservesExposureIdentity()
        {
            var exposure = CreateExposure(
                CreateSet(1, 100m, 8, 2));

            var result = ProgressExposureAnalyzer.Analyze(exposure);

            Assert.Equal(exposure.ExerciseSessionId, result.ExerciseSessionId);
            Assert.Equal(exposure.SessionId, result.SessionId);
            Assert.Equal(exposure.ExerciseId, result.ExerciseId);
            Assert.Equal(exposure.StartedAt, result.StartedAt);
            Assert.Equal(exposure.Prescription, result.Prescription);
        }

        [Fact]
        public void Analyze_UsesExplicitPlannedSetNumbersAndDoesNotReplaceMissingSetsWithExtras()
        {
            var exposure = CreateExposure(
                CreateSet(1, 100m, 8, 2),
                CreateSet(2, 100m, 8, 2),
                CreateSet(4, 100m, 8, 2),
                CreateSet(5, 100m, 20, 0));

            var result = ProgressExposureAnalyzer.Analyze(exposure);

            Assert.Equal(ExposureCompleteness.Partial, result.Completeness);
            Assert.Equal(3, result.CompletedPlannedSets);
            Assert.Equal(new[] { 1, 2, 4 }, result.EvaluatedSets.Select(x => x.SetNumber));
            Assert.Equal(new[] { 5 }, result.ExtraSets.Select(x => x.SetNumber));
            Assert.Equal(new[] { 3 }, result.MissingPlannedSetNumbers);
            Assert.False(result.IsOverloaded);
        }

        [Fact]
        public void Analyze_ClassifiesFullExposure()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(4, 100m, 8, 2),
                CreateSet(2, 100m, 8, 2),
                CreateSet(1, 100m, 8, 2),
                CreateSet(3, 100m, 8, 2)));

            Assert.Equal(ExposureCompleteness.Full, result.Completeness);
            Assert.Equal(4, result.CompletedPlannedSets);
            Assert.Empty(result.MissingPlannedSetNumbers);
            Assert.Equal(new[] { 1, 2, 3, 4 }, result.EvaluatedSets.Select(x => x.SetNumber));
        }

        [Theory]
        [InlineData(5, RepClassification.BelowRange)]
        [InlineData(6, RepClassification.InRange)]
        [InlineData(9, RepClassification.InRange)]
        [InlineData(10, RepClassification.TopRange)]
        [InlineData(11, RepClassification.TopRange)]
        public void Analyze_ClassifiesRepetitions(
            int repetitions,
            RepClassification expected)
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, repetitions, 2)));

            Assert.Equal(expected, result.EvaluatedSets.Single().RepClassification);
        }

        [Theory]
        [InlineData(null, RirClassification.Unknown)]
        [InlineData(0, RirClassification.HarderThanTarget)]
        [InlineData(1, RirClassification.OnTarget)]
        [InlineData(2, RirClassification.OnTarget)]
        [InlineData(3, RirClassification.EasierThanTarget)]
        public void Analyze_ClassifiesRir(
            int? rir,
            RirClassification expected)
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 8, rir)));

            Assert.Equal(expected, result.EvaluatedSets.Single().RirClassification);
        }

        [Fact]
        public void Analyze_CalculatesRirCoverageFromEvaluatedSetsOnly()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 8, 2),
                CreateSet(2, 100m, 8, null),
                CreateSet(3, 100m, 8, 1),
                CreateSet(4, 100m, 8, null),
                CreateSet(5, 100m, 8, 0)));

            Assert.Equal(0.5m, result.RirCoverage);
        }

        [Fact]
        public void Analyze_ClassifiesStableLoadAndSetsReferenceLoad()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 8, 2),
                CreateSet(2, 100m, 8, 2)));

            Assert.Equal(LoadClassification.StableLoad, result.LoadClassification);
            Assert.Equal(100m, result.ReferenceLoad);
        }

        [Fact]
        public void Analyze_ClassifiesZeroLoadAndSetsZeroReferenceLoad()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 0m, 8, 2),
                CreateSet(2, 0m, 8, 2)));

            Assert.Equal(LoadClassification.ZeroLoad, result.LoadClassification);
            Assert.Equal(0m, result.ReferenceLoad);
        }

        [Theory]
        [MemberData(nameof(MixedLoadSets))]
        public void Analyze_ClassifiesMixedLoad(
            IReadOnlyCollection<ProgressCompletedSet> sets)
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                sets.ToArray()));

            Assert.Equal(LoadClassification.MixedLoad, result.LoadClassification);
            Assert.Null(result.ReferenceLoad);
        }

        [Fact]
        public void Analyze_IgnoresExtraSetsForLoadAndPerformanceSignals()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 10, 2),
                CreateSet(2, 100m, 10, 2),
                CreateSet(3, 100m, 10, 2),
                CreateSet(4, 100m, 10, 2),
                CreateSet(5, 50m, 1, 0)));

            Assert.Equal(LoadClassification.StableLoad, result.LoadClassification);
            Assert.Equal(100m, result.ReferenceLoad);
            Assert.True(result.IsTopMastery);
            Assert.False(result.IsOverloaded);
        }

        [Fact]
        public void Analyze_DetectsTopMasteryWithUnknownAndEasierRir()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 10, null),
                CreateSet(2, 100m, 10, 3),
                CreateSet(3, 100m, 11, 2),
                CreateSet(4, 100m, 10, 1)));

            Assert.True(result.IsTopMastery);
        }

        [Fact]
        public void Analyze_DoesNotDetectTopMasteryForPartialExposure()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 10, 2),
                CreateSet(2, 100m, 10, 2),
                CreateSet(3, 100m, 10, 2)));

            Assert.False(result.IsTopMastery);
        }

        [Theory]
        [InlineData(9, 1)]
        [InlineData(10, 0)]
        public void Analyze_DoesNotDetectTopMasteryWhenASetFailsARequirement(
            int repetitions,
            int rir)
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, repetitions, rir),
                CreateSet(2, 100m, 10, 2),
                CreateSet(3, 100m, 10, 2),
                CreateSet(4, 100m, 10, 2)));

            Assert.False(result.IsTopMastery);
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, false)]
        public void Analyze_AppliesRepetitionOverloadThreshold(
            int belowRangeSets,
            bool expected)
        {
            var sets = Enumerable.Range(1, 4)
                .Select(number => CreateSet(
                    number,
                    100m,
                    number <= belowRangeSets ? 5 : 6,
                    2))
                .ToArray();

            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(sets));

            Assert.Equal(expected, result.HasRepetitionOverload);
            Assert.Equal(expected, result.IsOverloaded);
        }

        [Fact]
        public void Analyze_DetectsRirOverloadAtCoverageAndHarderThresholds()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 8, 0),
                CreateSet(2, 100m, 8, 1),
                CreateSet(3, 100m, 8, null),
                CreateSet(4, 100m, 8, null)));

            Assert.Equal(0.5m, result.RirCoverage);
            Assert.True(result.HasRirOverload);
            Assert.True(result.IsOverloaded);
        }

        [Fact]
        public void Analyze_DoesNotDetectRirOverloadBelowCoverageThreshold()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 8, 0),
                CreateSet(2, 100m, 8, null),
                CreateSet(3, 100m, 8, null),
                CreateSet(4, 100m, 8, null)));

            Assert.Equal(0.25m, result.RirCoverage);
            Assert.False(result.HasRirOverload);
            Assert.False(result.IsOverloaded);
        }

        [Fact]
        public void Analyze_DoesNotDetectRirOverloadWhenHarderRatioIsBelowThreshold()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(1, 100m, 8, 0),
                CreateSet(2, 100m, 8, 1),
                CreateSet(3, 100m, 8, 2),
                CreateSet(4, 100m, 8, null)));

            Assert.Equal(0.75m, result.RirCoverage);
            Assert.False(result.HasRirOverload);
            Assert.False(result.IsOverloaded);
        }

        [Fact]
        public void Analyze_ReturnsUnavailableRirAndLoadObservationsWhenOnlyExtraSetsExist()
        {
            var result = ProgressExposureAnalyzer.Analyze(CreateExposure(
                CreateSet(5, 100m, 10, 2)));

            Assert.Equal(ExposureCompleteness.Partial, result.Completeness);
            Assert.Empty(result.EvaluatedSets);
            Assert.Single(result.ExtraSets);
            Assert.Equal(new[] { 1, 2, 3, 4 }, result.MissingPlannedSetNumbers);
            Assert.Null(result.RirCoverage);
            Assert.Null(result.LoadClassification);
            Assert.Null(result.ReferenceLoad);
            Assert.False(result.IsTopMastery);
            Assert.False(result.IsOverloaded);
        }

        public static TheoryData<IReadOnlyCollection<ProgressCompletedSet>>
            MixedLoadSets => new()
            {
                {
                    new[]
                    {
                        CreateSet(1, 100m, 10, 2),
                        CreateSet(2, 95m, 10, 2)
                    }
                },
                {
                    new[]
                    {
                        CreateSet(1, 0m, 10, 2),
                        CreateSet(2, 10m, 10, 2)
                    }
                }
            };

        private static ProgressExposure CreateExposure(
            params ProgressCompletedSet[] sets)
        {
            return new ProgressExposure(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc),
                new ProgressPrescriptionSnapshot(
                    4,
                    6,
                    10,
                    1,
                    2,
                    120),
                sets);
        }

        private static ProgressCompletedSet CreateSet(
            int setNumber,
            decimal weight,
            int repetitions,
            int? rir)
        {
            return new ProgressCompletedSet(
                setNumber,
                weight,
                repetitions,
                rir);
        }
    }
}
