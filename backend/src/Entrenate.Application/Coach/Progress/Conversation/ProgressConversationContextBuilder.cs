using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;

namespace Entrenate.Application.Coach.Progress.Conversation
{
    public static class ProgressConversationContextBuilder
    {
        public static ProgressConversationContext Build(
            ProgressAnalysis analysis,
            ProgressRecommendation recommendation,
            ProgressExplanation explanation)
        {
            ArgumentNullException.ThrowIfNull(analysis);
            ArgumentNullException.ThrowIfNull(recommendation);
            ArgumentNullException.ThrowIfNull(explanation);

            if (analysis.ExerciseId != recommendation.ExerciseId)
            {
                throw new ArgumentException(
                    "El análisis y la recomendación deben pertenecer al " +
                        "mismo ejercicio.",
                    nameof(recommendation));
            }

            var latest = analysis.LatestExposure;

            return new ProgressConversationContext(
                analysis.ExerciseId,
                analysis.ValidExposureCount,
                analysis.DistinctSessionCount,
                analysis.ComparableExposureCount,
                analysis.PrescriptionChanged,
                analysis.LoadChanged,
                latest is null
                    ? null
                    : new ProgressConversationPrescription(
                        latest.Prescription.PlannedSets,
                        latest.Prescription.MinReps,
                        latest.Prescription.MaxReps,
                        latest.Prescription.MinRir,
                        latest.Prescription.MaxRir),
                MapLatestExposure(latest),
                recommendation.Decision,
                recommendation.EvidenceLevel,
                recommendation.ReasonCodes.ToArray(),
                explanation);
        }

        private static ProgressConversationLatestExposure?
            MapLatestExposure(ProgressExposureAnalysis? exposure)
        {
            if (exposure is null)
            {
                return null;
            }

            return new ProgressConversationLatestExposure(
                exposure.StartedAt,
                exposure.Completeness == ExposureCompleteness.Full,
                exposure.CompletedPlannedSets,
                exposure.MissingPlannedSetNumbers.Count,
                exposure.ExtraSets.Count,
                exposure.EvaluatedSets.Count(set =>
                    set.RepClassification == RepClassification.BelowRange),
                exposure.EvaluatedSets.Count(set =>
                    set.RepClassification == RepClassification.InRange),
                exposure.EvaluatedSets.Count(set =>
                    set.RepClassification == RepClassification.TopRange),
                exposure.RirCoverage,
                exposure.LoadClassification,
                exposure.ReferenceLoad,
                exposure.HasRepetitionOverload,
                exposure.HasRirOverload,
                exposure.IsTopMastery,
                exposure.IsOverloaded);
        }
    }
}
