using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress;
using Entrenate.Application.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgress
{
    public class GetExerciseProgressQueryHandler
        : IRequestHandler<GetExerciseProgressQuery, ExerciseProgressDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetExerciseProgressQueryHandler(
            ICurrentUserService currentUserService,
            IExerciseRepository exerciseRepository,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _exerciseRepository = exerciseRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<ExerciseProgressDto> Handle(
            GetExerciseProgressQuery request,
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

            var entries = await _sessionRepository
                .GetCompletedExerciseProgressByUserIdAsync(
                    request.ExerciseId,
                    userId,
                    cancellationToken);

            var entriesWithMetrics = entries
                .Select(ExerciseProgressMetricsCalculator.Calculate)
                .ToList();

            return new ExerciseProgressDto(
                exercise.Id,
                exercise.Nombre,
                entriesWithMetrics,
                ExerciseProgressSummaryCalculator.Calculate(
                    entriesWithMetrics));
        }
    }
}
