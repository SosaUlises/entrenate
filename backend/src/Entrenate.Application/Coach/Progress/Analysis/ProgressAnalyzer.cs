namespace Entrenate.Application.Coach.Progress.Analysis
{
    public static class ProgressAnalyzer
    {
        public static ProgressAnalysis Analyze(
            Guid exerciseId,
            IReadOnlyCollection<ProgressExposure> exposures)
        {
            ArgumentNullException.ThrowIfNull(exposures);

            var analyzedExposures = exposures
                .Select(ProgressExposureAnalyzer.Analyze)
                .ToList();

            var latestExposure = analyzedExposures.FirstOrDefault();

            if (latestExposure is null)
            {
                return new ProgressAnalysis(
                    exerciseId,
                    0,
                    0,
                    analyzedExposures,
                    null,
                    [],
                    0,
                    false,
                    false);
            }

            var sessionGroups = analyzedExposures
                .GroupBy(exposure => exposure.SessionId)
                .ToList();

            var comparableExposures = BuildComparableExposureSequence(
                latestExposure,
                sessionGroups);

            var previousSession = sessionGroups
                .FirstOrDefault(group =>
                    group.Key != latestExposure.SessionId);

            var prescriptionChanged = HasPrescriptionChanged(
                latestExposure,
                previousSession);

            var loadChanged = HasUnambiguousLoadChanged(
                latestExposure,
                previousSession);

            return new ProgressAnalysis(
                exerciseId,
                analyzedExposures.Count,
                sessionGroups.Count,
                analyzedExposures,
                latestExposure,
                comparableExposures,
                comparableExposures.Count,
                prescriptionChanged,
                loadChanged);
        }

        private static IReadOnlyCollection<ProgressExposureAnalysis>
            BuildComparableExposureSequence(
                ProgressExposureAnalysis latestExposure,
                IReadOnlyCollection<
                    IGrouping<Guid, ProgressExposureAnalysis>> sessionGroups)
        {
            if (!CanParticipateInLoadComparison(latestExposure))
            {
                return [];
            }

            var comparableExposures = new List<ProgressExposureAnalysis>
            {
                latestExposure
            };

            foreach (var sessionGroup in sessionGroups)
            {
                if (sessionGroup.Key == latestExposure.SessionId)
                {
                    continue;
                }

                var comparableExposure = sessionGroup.FirstOrDefault(
                    exposure => AreComparable(
                        latestExposure,
                        exposure));

                if (comparableExposure is null)
                {
                    break;
                }

                comparableExposures.Add(comparableExposure);
            }

            return comparableExposures;
        }

        private static bool AreComparable(
            ProgressExposureAnalysis latestExposure,
            ProgressExposureAnalysis candidate)
        {
            return latestExposure.SessionId != candidate.SessionId &&
                latestExposure.ExerciseId == candidate.ExerciseId &&
                CanParticipateInLoadComparison(candidate) &&
                HasSameRelevantPrescription(latestExposure, candidate) &&
                latestExposure.ReferenceLoad == candidate.ReferenceLoad;
        }

        private static bool CanParticipateInLoadComparison(
            ProgressExposureAnalysis exposure)
        {
            return exposure.Completeness == ExposureCompleteness.Full &&
                exposure.LoadClassification ==
                    LoadClassification.StableLoad &&
                exposure.ReferenceLoad.HasValue;
        }

        private static bool HasPrescriptionChanged(
            ProgressExposureAnalysis latestExposure,
            IEnumerable<ProgressExposureAnalysis>? previousSession)
        {
            if (previousSession is null)
            {
                return false;
            }

            var sameExerciseExposures = previousSession
                .Where(exposure =>
                    exposure.ExerciseId == latestExposure.ExerciseId)
                .ToList();

            return sameExerciseExposures.Count > 0 &&
                sameExerciseExposures.All(exposure =>
                    !HasSameRelevantPrescription(
                        latestExposure,
                        exposure));
        }

        private static bool HasUnambiguousLoadChanged(
            ProgressExposureAnalysis latestExposure,
            IEnumerable<ProgressExposureAnalysis>? previousSession)
        {
            if (previousSession is null ||
                latestExposure.LoadClassification !=
                    LoadClassification.StableLoad ||
                !latestExposure.ReferenceLoad.HasValue)
            {
                return false;
            }

            var previousReferenceLoads = previousSession
                .Where(exposure =>
                    exposure.ExerciseId == latestExposure.ExerciseId &&
                    exposure.LoadClassification ==
                        LoadClassification.StableLoad &&
                    exposure.ReferenceLoad.HasValue &&
                    HasSameRelevantPrescription(
                        latestExposure,
                        exposure))
                .Select(exposure => exposure.ReferenceLoad!.Value)
                .Distinct()
                .ToList();

            return previousReferenceLoads.Count == 1 &&
                previousReferenceLoads[0] !=
                    latestExposure.ReferenceLoad.Value;
        }

        private static bool HasSameRelevantPrescription(
            ProgressExposureAnalysis first,
            ProgressExposureAnalysis second)
        {
            return first.Prescription.PlannedSets ==
                    second.Prescription.PlannedSets &&
                first.Prescription.MinReps ==
                    second.Prescription.MinReps &&
                first.Prescription.MaxReps ==
                    second.Prescription.MaxReps &&
                first.Prescription.MinRir ==
                    second.Prescription.MinRir &&
                first.Prescription.MaxRir ==
                    second.Prescription.MaxRir;
        }
    }
}
