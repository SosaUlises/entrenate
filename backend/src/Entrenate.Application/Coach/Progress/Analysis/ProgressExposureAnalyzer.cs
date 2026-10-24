namespace Entrenate.Application.Coach.Progress.Analysis
{
    public static class ProgressExposureAnalyzer
    {
        private const decimal OverloadThreshold = 0.5m;

        public static ProgressExposureAnalysis Analyze(
            ProgressExposure exposure)
        {
            ArgumentNullException.ThrowIfNull(exposure);

            var evaluatedSets = exposure.CompletedSets
                .Where(set =>
                    set.SetNumber >= 1 &&
                    set.SetNumber <= exposure.Prescription.PlannedSets)
                .OrderBy(set => set.SetNumber)
                .Select(set => AnalyzeSet(set, exposure.Prescription))
                .ToList();

            var extraSets = exposure.CompletedSets
                .Where(set =>
                    set.SetNumber > exposure.Prescription.PlannedSets)
                .OrderBy(set => set.SetNumber)
                .ToList();

            var completedPlannedSetNumbers = evaluatedSets
                .Select(set => set.SetNumber)
                .ToHashSet();

            var missingPlannedSetNumbers = Enumerable
                .Range(1, exposure.Prescription.PlannedSets)
                .Where(setNumber =>
                    !completedPlannedSetNumbers.Contains(setNumber))
                .ToList();

            var completeness = evaluatedSets.Count ==
                exposure.Prescription.PlannedSets
                    ? ExposureCompleteness.Full
                    : ExposureCompleteness.Partial;

            var knownRirSets = evaluatedSets
                .Where(set => set.Rir.HasValue)
                .ToList();

            var rirCoverage = evaluatedSets.Count == 0
                ? (decimal?)null
                : (decimal)knownRirSets.Count / evaluatedSets.Count;

            var loadClassification = ClassifyLoad(evaluatedSets);
            var referenceLoad = loadClassification ==
                LoadClassification.StableLoad
                    ? evaluatedSets[0].Weight
                    : loadClassification == LoadClassification.ZeroLoad
                        ? 0m
                        : (decimal?)null;

            var belowRangeCount = evaluatedSets.Count(set =>
                set.RepClassification == RepClassification.BelowRange);

            var hasRepetitionOverload = evaluatedSets.Count > 0 &&
                (decimal)belowRangeCount / evaluatedSets.Count >=
                    OverloadThreshold;

            var harderThanTargetCount = knownRirSets.Count(set =>
                set.RirClassification ==
                    RirClassification.HarderThanTarget);

            var hasRirOverload = rirCoverage >= OverloadThreshold &&
                knownRirSets.Count > 0 &&
                (decimal)harderThanTargetCount / knownRirSets.Count >=
                    OverloadThreshold;

            var isTopMastery =
                completeness == ExposureCompleteness.Full &&
                evaluatedSets.All(set =>
                    set.RepClassification == RepClassification.TopRange) &&
                evaluatedSets.All(set =>
                    set.RirClassification !=
                        RirClassification.HarderThanTarget);

            return new ProgressExposureAnalysis(
                exposure.ExerciseSessionId,
                exposure.SessionId,
                exposure.ExerciseId,
                exposure.StartedAt,
                exposure.Prescription,
                completeness,
                evaluatedSets.Count,
                evaluatedSets,
                extraSets,
                missingPlannedSetNumbers,
                rirCoverage,
                loadClassification,
                referenceLoad,
                hasRepetitionOverload,
                hasRirOverload,
                isTopMastery,
                hasRepetitionOverload || hasRirOverload);
        }

        private static ProgressEvaluatedSetAnalysis AnalyzeSet(
            ProgressCompletedSet set,
            ProgressPrescriptionSnapshot prescription)
        {
            return new ProgressEvaluatedSetAnalysis(
                set.SetNumber,
                set.Weight,
                set.Repetitions,
                set.Rir,
                ClassifyRepetitions(set.Repetitions, prescription),
                ClassifyRir(set.Rir, prescription));
        }

        private static RepClassification ClassifyRepetitions(
            int repetitions,
            ProgressPrescriptionSnapshot prescription)
        {
            if (repetitions < prescription.MinReps)
            {
                return RepClassification.BelowRange;
            }

            return repetitions < prescription.MaxReps
                ? RepClassification.InRange
                : RepClassification.TopRange;
        }

        private static RirClassification ClassifyRir(
            int? rir,
            ProgressPrescriptionSnapshot prescription)
        {
            if (!rir.HasValue)
            {
                return RirClassification.Unknown;
            }

            if (rir.Value < prescription.MinRir)
            {
                return RirClassification.HarderThanTarget;
            }

            return rir.Value <= prescription.MaxRir
                ? RirClassification.OnTarget
                : RirClassification.EasierThanTarget;
        }

        private static LoadClassification? ClassifyLoad(
            IReadOnlyList<ProgressEvaluatedSetAnalysis> evaluatedSets)
        {
            if (evaluatedSets.Count == 0)
            {
                return null;
            }

            if (evaluatedSets.All(set => set.Weight == 0m))
            {
                return LoadClassification.ZeroLoad;
            }

            var referenceLoad = evaluatedSets[0].Weight;

            return referenceLoad > 0m &&
                evaluatedSets.All(set => set.Weight == referenceLoad)
                    ? LoadClassification.StableLoad
                    : LoadClassification.MixedLoad;
        }
    }
}
