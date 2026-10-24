using Entrenate.Application.Coach.Progress.Analysis;

namespace Entrenate.Application.Coach.Progress.Recommendations
{
    public static class ProgressRecommendationEngine
    {
        private const decimal SufficientRirCoverage = 0.5m;

        public static ProgressRecommendation Recommend(
            ProgressAnalysis analysis)
        {
            ArgumentNullException.ThrowIfNull(analysis);

            var latest = analysis.LatestExposure;

            if (latest is null ||
                analysis.ValidExposureCount == 0 ||
                latest.EvaluatedSets.Count == 0 ||
                (analysis.ValidExposureCount == 1 &&
                    latest.Completeness == ExposureCompleteness.Partial))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.InsufficientData,
                    EvidenceLevel.Low,
                    ReasonCode.InsufficientHistory);
            }

            var comparable = analysis.ComparableExposures.ToList();

            if (HasReduceLoadWithRir(comparable))
            {
                var relevant = comparable.Take(3).ToList();
                var evidence = relevant.Count == 3 &&
                    relevant.All(exposure => exposure.IsOverloaded) &&
                    CalculateCombinedRirCoverage(relevant) >=
                        SufficientRirCoverage
                            ? EvidenceLevel.High
                            : EvidenceLevel.Medium;

                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReduceLoad,
                    evidence);
            }

            if (HasReduceLoadWithoutRir(comparable))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReduceLoad,
                    EvidenceLevel.Medium);
            }

            if (HasIncreaseLoadWithRir(comparable))
            {
                var relevant = comparable.Take(3).ToList();
                var evidence = relevant.Count == 3 &&
                    relevant.All(exposure => exposure.IsTopMastery) &&
                    CalculateCombinedRirCoverage(relevant) >=
                        SufficientRirCoverage
                            ? EvidenceLevel.High
                            : EvidenceLevel.Medium;

                return CreateRecommendation(
                    analysis,
                    ProgressDecision.IncreaseLoad,
                    evidence,
                    ReasonCode.TopRangeConsistent);
            }

            if (HasIncreaseLoadWithoutRir(comparable))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.IncreaseLoad,
                    EvidenceLevel.Medium,
                    ReasonCode.TopRangeConsistent);
            }

            var distinctSessionExposures = GetDistinctSessionExposures(
                analysis.Exposures);

            if (latest.IsOverloaded)
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReviewPerformance,
                    EvidenceLevel.Low,
                    ReasonCode.RecentPerformanceDrop);
            }

            if (distinctSessionExposures.Count >= 2 &&
                distinctSessionExposures.Take(2).All(exposure =>
                    exposure.Completeness == ExposureCompleteness.Partial))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReviewPerformance,
                    EvidenceLevel.Medium,
                    ReasonCode.RepeatedPartialExposure);
            }

            if (HasContradictoryRecentPerformance(comparable))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReviewPerformance,
                    EvidenceLevel.Medium,
                    ReasonCode.RecentPerformanceDrop);
            }

            var relevantContext = BuildRelevantContextSequence(
                latest,
                analysis.Exposures);

            if (HasRepeatedTopMastery(
                relevantContext,
                LoadClassification.ZeroLoad))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReviewPerformance,
                    GetRepeatedSignalEvidence(relevantContext),
                    ReasonCode.TopRangeConsistent);
            }

            if (HasRepeatedMixedLoad(
                latest,
                distinctSessionExposures))
            {
                return CreateRecommendation(
                    analysis,
                    ProgressDecision.ReviewPerformance,
                    EvidenceLevel.Medium);
            }

            if (CanIncreaseRepetitions(latest))
            {
                var consistentExposureCount = relevantContext
                    .TakeWhile(CanIncreaseRepetitions)
                    .Count();

                return CreateRecommendation(
                    analysis,
                    ProgressDecision.IncreaseRepetitions,
                    GetConsistentEvidenceLevel(
                        relevantContext,
                        consistentExposureCount));
            }

            return CreateRecommendation(
                analysis,
                ProgressDecision.Maintain,
                GetMaintainEvidenceLevel(relevantContext));
        }

        private static bool HasReduceLoadWithRir(
            IReadOnlyCollection<ProgressExposureAnalysis> comparable)
        {
            var latestTwo = comparable.Take(2).ToList();

            return latestTwo.Count == 2 &&
                latestTwo.All(exposure => exposure.IsOverloaded) &&
                CalculateCombinedRirCoverage(latestTwo) >=
                    SufficientRirCoverage;
        }

        private static bool HasReduceLoadWithoutRir(
            IReadOnlyCollection<ProgressExposureAnalysis> comparable)
        {
            var latestThree = comparable.Take(3).ToList();

            return latestThree.Count == 3 &&
                latestThree.All(exposure =>
                    exposure.HasRepetitionOverload) &&
                CalculateCombinedRirCoverage(latestThree) <
                    SufficientRirCoverage;
        }

        private static bool HasIncreaseLoadWithRir(
            IReadOnlyCollection<ProgressExposureAnalysis> comparable)
        {
            var latestTwo = comparable.Take(2).ToList();

            return latestTwo.Count == 2 &&
                latestTwo.All(exposure => exposure.IsTopMastery) &&
                CalculateCombinedRirCoverage(latestTwo) >=
                    SufficientRirCoverage;
        }

        private static bool HasIncreaseLoadWithoutRir(
            IReadOnlyCollection<ProgressExposureAnalysis> comparable)
        {
            var latestThree = comparable.Take(3).ToList();

            return latestThree.Count == 3 &&
                latestThree.All(exposure => exposure.IsTopMastery) &&
                CalculateCombinedRirCoverage(latestThree) <
                    SufficientRirCoverage;
        }

        private static decimal CalculateCombinedRirCoverage(
            IReadOnlyCollection<ProgressExposureAnalysis> exposures)
        {
            var evaluatedSets = exposures
                .SelectMany(exposure => exposure.EvaluatedSets)
                .ToList();

            if (evaluatedSets.Count == 0)
            {
                return 0m;
            }

            var knownRirSets = evaluatedSets.Count(set => set.Rir.HasValue);

            return (decimal)knownRirSets / evaluatedSets.Count;
        }

        private static IReadOnlyList<ProgressExposureAnalysis>
            GetDistinctSessionExposures(
                IReadOnlyCollection<ProgressExposureAnalysis> exposures)
        {
            return exposures
                .GroupBy(exposure => exposure.SessionId)
                .Select(group => group.First())
                .ToList();
        }

        private static bool HasContradictoryRecentPerformance(
            IReadOnlyCollection<ProgressExposureAnalysis> comparable)
        {
            var latestTwo = comparable.Take(2).ToList();

            return latestTwo.Count == 2 &&
                ((latestTwo[0].IsTopMastery &&
                    latestTwo[1].IsOverloaded) ||
                (latestTwo[0].IsOverloaded &&
                    latestTwo[1].IsTopMastery));
        }

        private static IReadOnlyList<ProgressExposureAnalysis>
            BuildRelevantContextSequence(
                ProgressExposureAnalysis latest,
                IReadOnlyCollection<ProgressExposureAnalysis> exposures)
        {
            var relevant = new List<ProgressExposureAnalysis>
            {
                latest
            };

            foreach (var session in exposures
                .GroupBy(exposure => exposure.SessionId)
                .Where(group => group.Key != latest.SessionId))
            {
                var matchingExposure = session.FirstOrDefault(exposure =>
                    exposure.ExerciseId == latest.ExerciseId &&
                    HasSameRelevantPrescription(latest, exposure) &&
                    HasSameLoadContext(latest, exposure));

                if (matchingExposure is null)
                {
                    break;
                }

                relevant.Add(matchingExposure);
            }

            return relevant;
        }

        private static bool HasSameRelevantPrescription(
            ProgressExposureAnalysis first,
            ProgressExposureAnalysis second)
        {
            return first.Prescription.PlannedSets ==
                    second.Prescription.PlannedSets &&
                first.Prescription.MinReps == second.Prescription.MinReps &&
                first.Prescription.MaxReps == second.Prescription.MaxReps &&
                first.Prescription.MinRir == second.Prescription.MinRir &&
                first.Prescription.MaxRir == second.Prescription.MaxRir;
        }

        private static bool HasSameLoadContext(
            ProgressExposureAnalysis first,
            ProgressExposureAnalysis second)
        {
            if (first.LoadClassification == LoadClassification.StableLoad)
            {
                return second.LoadClassification ==
                        LoadClassification.StableLoad &&
                    first.ReferenceLoad == second.ReferenceLoad;
            }

            return first.LoadClassification == LoadClassification.ZeroLoad &&
                second.LoadClassification == LoadClassification.ZeroLoad;
        }

        private static bool HasRepeatedTopMastery(
            IReadOnlyCollection<ProgressExposureAnalysis> relevantContext,
            LoadClassification loadClassification)
        {
            return relevantContext.Count >= 2 &&
                relevantContext.Take(2).All(exposure =>
                    exposure.LoadClassification == loadClassification &&
                    exposure.IsTopMastery);
        }

        private static bool HasRepeatedMixedLoad(
            ProgressExposureAnalysis latest,
            IReadOnlyCollection<ProgressExposureAnalysis>
                distinctSessionExposures)
        {
            var latestTwo = distinctSessionExposures.Take(2).ToList();

            return latestTwo.Count == 2 &&
                latestTwo.All(exposure =>
                    exposure.ExerciseId == latest.ExerciseId &&
                    exposure.LoadClassification ==
                        LoadClassification.MixedLoad &&
                    HasSameRelevantPrescription(latest, exposure));
        }

        private static bool CanIncreaseRepetitions(
            ProgressExposureAnalysis exposure)
        {
            return exposure.Completeness == ExposureCompleteness.Full &&
                exposure.EvaluatedSets.All(set =>
                    set.RepClassification != RepClassification.BelowRange) &&
                exposure.EvaluatedSets.Any(set =>
                    set.RepClassification == RepClassification.InRange);
        }

        private static EvidenceLevel GetRepeatedSignalEvidence(
            IReadOnlyCollection<ProgressExposureAnalysis> relevantContext)
        {
            return relevantContext.Count >= 3 &&
                relevantContext.Take(3).All(exposure =>
                    exposure.IsTopMastery &&
                    exposure.Completeness == ExposureCompleteness.Full)
                        ? EvidenceLevel.High
                        : EvidenceLevel.Medium;
        }

        private static EvidenceLevel GetConsistentEvidenceLevel(
            IReadOnlyCollection<ProgressExposureAnalysis> relevantContext,
            int consistentExposureCount)
        {
            if (consistentExposureCount >= 3 &&
                relevantContext.Take(2).All(exposure =>
                    exposure.Completeness == ExposureCompleteness.Full))
            {
                return EvidenceLevel.High;
            }

            return consistentExposureCount >= 2
                ? EvidenceLevel.Medium
                : EvidenceLevel.Low;
        }

        private static EvidenceLevel GetMaintainEvidenceLevel(
            IReadOnlyCollection<ProgressExposureAnalysis> relevantContext)
        {
            if (relevantContext.Count < 2)
            {
                return EvidenceLevel.Low;
            }

            var latestState = GetStrongState(relevantContext.First());
            var consistentCount = relevantContext
                .TakeWhile(exposure =>
                    GetStrongState(exposure) == latestState)
                .Count();

            return GetConsistentEvidenceLevel(
                relevantContext,
                consistentCount);
        }

        private static int GetStrongState(
            ProgressExposureAnalysis exposure)
        {
            if (exposure.IsTopMastery)
            {
                return 1;
            }

            if (exposure.IsOverloaded)
            {
                return 2;
            }

            if (exposure.Completeness == ExposureCompleteness.Partial)
            {
                return 3;
            }

            return 0;
        }

        private static ProgressRecommendation CreateRecommendation(
            ProgressAnalysis analysis,
            ProgressDecision decision,
            EvidenceLevel evidenceLevel,
            params ReasonCode[] additionalReasons)
        {
            var reasons = BuildReasonCodes(
                analysis,
                additionalReasons);

            return new ProgressRecommendation(
                analysis.ExerciseId,
                decision,
                evidenceLevel,
                reasons);
        }

        private static IReadOnlyCollection<ReasonCode> BuildReasonCodes(
            ProgressAnalysis analysis,
            IReadOnlyCollection<ReasonCode> additionalReasons)
        {
            var reasons = new HashSet<ReasonCode>(additionalReasons);
            var latest = analysis.LatestExposure;

            if (latest is not null)
            {
                AddExposureReasons(reasons, latest);
            }

            if (analysis.PrescriptionChanged)
            {
                reasons.Add(ReasonCode.PrescriptionChanged);
            }

            if (analysis.LoadChanged)
            {
                reasons.Add(ReasonCode.LoadChanged);
            }

            return reasons
                .OrderBy(reason => (int)reason)
                .ToList();
        }

        private static void AddExposureReasons(
            ISet<ReasonCode> reasons,
            ProgressExposureAnalysis exposure)
        {
            if (exposure.Completeness == ExposureCompleteness.Partial)
            {
                reasons.Add(ReasonCode.PartialExposure);
            }

            if (exposure.EvaluatedSets.Any(set =>
                set.RepClassification == RepClassification.BelowRange))
            {
                reasons.Add(ReasonCode.BelowMinimumReps);
            }
            else if (exposure.EvaluatedSets.All(set =>
                set.RepClassification == RepClassification.TopRange))
            {
                reasons.Add(ReasonCode.TopRangeReached);
            }
            else if (exposure.EvaluatedSets.Count > 0)
            {
                reasons.Add(ReasonCode.WithinRepRange);
            }

            if (exposure.EvaluatedSets.Any(set =>
                set.RirClassification ==
                    RirClassification.HarderThanTarget))
            {
                reasons.Add(ReasonCode.RirTooLow);
            }

            if (exposure.EvaluatedSets.Any(set =>
                set.RirClassification == RirClassification.OnTarget))
            {
                reasons.Add(ReasonCode.RirOnTarget);
            }

            if (exposure.EvaluatedSets.Any(set =>
                set.RirClassification == RirClassification.Unknown))
            {
                reasons.Add(ReasonCode.RirMissing);
            }

            switch (exposure.LoadClassification)
            {
                case LoadClassification.StableLoad:
                    reasons.Add(ReasonCode.StableLoad);
                    break;
                case LoadClassification.MixedLoad:
                    reasons.Add(ReasonCode.MixedLoad);
                    break;
                case LoadClassification.ZeroLoad:
                    reasons.Add(ReasonCode.ZeroExternalLoad);
                    break;
            }
        }
    }
}
