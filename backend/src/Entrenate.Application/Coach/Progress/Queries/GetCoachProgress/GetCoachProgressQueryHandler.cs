using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.Progress.DTOs;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.Progress.Queries.GetCoachProgress
{
    public class GetCoachProgressQueryHandler
        : IRequestHandler<GetCoachProgressQuery, CoachProgressDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetCoachProgressQueryHandler(
            ICurrentUserService currentUserService,
            IExerciseRepository exerciseRepository,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _exerciseRepository = exerciseRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<CoachProgressDto> Handle(
            GetCoachProgressQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var exercise = await _exerciseRepository.GetByIdAsync(
                request.ExerciseId,
                cancellationToken);

            if (exercise is null)
            {
                throw new NotFoundException("El ejercicio no existe.");
            }

            var exposures = await _sessionRepository
                .GetRecentCompletedExerciseExposuresByUserIdAsync(
                    exercise.Id,
                    userId,
                    cancellationToken);

            var analysis = ProgressAnalyzer.Analyze(
                exercise.Id,
                exposures);
            var recommendation = ProgressRecommendationEngine.Recommend(
                analysis);
            var explanation = ProgressExplanationGenerator.Generate(
                recommendation,
                analysis);

            return Map(analysis, recommendation, explanation);
        }

        private static CoachProgressDto Map(
            ProgressAnalysis analysis,
            ProgressRecommendation recommendation,
            ProgressExplanation explanation)
        {
            return new CoachProgressDto(
                analysis.ExerciseId,
                new CoachProgressAnalysisDto(
                    analysis.ValidExposureCount,
                    analysis.DistinctSessionCount,
                    analysis.ComparableExposureCount,
                    analysis.PrescriptionChanged,
                    analysis.LoadChanged,
                    MapLatestExposure(analysis.LatestExposure)),
                new CoachProgressRecommendationDto(
                    recommendation.Decision.ToString(),
                    recommendation.EvidenceLevel.ToString(),
                    recommendation.ReasonCodes
                        .Select(reason => reason.ToString())
                        .ToList()),
                new CoachProgressExplanationDto(
                    explanation.Detection,
                    explanation.Recommendation,
                    explanation.Rationale));
        }

        private static CoachProgressLatestExposureDto? MapLatestExposure(
            ProgressExposureAnalysis? exposure)
        {
            if (exposure is null)
            {
                return null;
            }

            return new CoachProgressLatestExposureDto(
                exposure.ExerciseSessionId,
                exposure.SessionId,
                exposure.StartedAt,
                exposure.Completeness == ExposureCompleteness.Full,
                exposure.CompletedPlannedSets,
                exposure.MissingPlannedSetNumbers.Count,
                exposure.ExtraSets.Count,
                exposure.LoadClassification?.ToString(),
                exposure.ReferenceLoad,
                exposure.RirCoverage,
                exposure.IsTopMastery,
                exposure.IsOverloaded,
                new CoachProgressPrescriptionDto(
                    exposure.Prescription.PlannedSets,
                    exposure.Prescription.MinReps,
                    exposure.Prescription.MaxReps,
                    exposure.Prescription.MinRir,
                    exposure.Prescription.MaxRir,
                    exposure.Prescription.RestSeconds));
        }
    }
}
