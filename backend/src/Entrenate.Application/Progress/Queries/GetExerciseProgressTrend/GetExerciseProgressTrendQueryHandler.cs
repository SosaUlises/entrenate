using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgressTrend
{
    public class GetExerciseProgressTrendQueryHandler
        : IRequestHandler<GetExerciseProgressTrendQuery, ExerciseProgressTrendDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IExerciseRepository _exerciseRepository;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetExerciseProgressTrendQueryHandler(
            ICurrentUserService currentUserService,
            IExerciseRepository exerciseRepository,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _exerciseRepository = exerciseRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<ExerciseProgressTrendDto> Handle(
            GetExerciseProgressTrendQuery request,
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

            DateTime? fromUtc = null;
            DateTime? toUtcExclusive = null;

            if (request.From.HasValue && request.To.HasValue)
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone!);
                fromUtc = GetUtcStartOfDay(request.From.Value, timeZone);
                toUtcExclusive = GetUtcStartOfDay(request.To.Value.AddDays(1), timeZone);
            }

            var entries = await _sessionRepository
                .GetCompletedExerciseProgressTrendByUserIdAsync(
                    request.ExerciseId,
                    userId,
                    fromUtc,
                    toUtcExclusive,
                    cancellationToken);

            var points = entries
                .Select(BuildPoint)
                .OrderBy(x => x.HoraInicio)
                .ThenBy(x => x.SessionId)
                .ToList();

            var hasHistory = points.Count > 0 ||
                (request.From.HasValue &&
                    await _sessionRepository.HasCompletedExerciseProgressByUserIdAsync(
                        request.ExerciseId,
                        userId,
                        cancellationToken));

            return new ExerciseProgressTrendDto(
                exercise.Id,
                exercise.Nombre,
                hasHistory,
                points);
        }

        private static ExerciseProgressTrendPointDto BuildPoint(
            ExerciseProgressEntryDto entry)
        {
            var calculated = ExerciseProgressMetricsCalculator.Calculate(entry);
            var completedSets = calculated.Series
                .Where(x => x.Completada)
                .ToList();
            var weightedSets = completedSets
                .Where(x => x.Peso > 0)
                .ToList();

            return new ExerciseProgressTrendPointDto(
                calculated.SessionId,
                calculated.HoraInicio,
                calculated.E1RmEstimado,
                weightedSets.Count > 0 ? weightedSets.Max(x => x.Peso) : null,
                completedSets.Max(x => x.Repeticiones),
                calculated.VolumenTotal,
                weightedSets.Count > 0
                    ? weightedSets.Max(x => x.Peso * x.Repeticiones)
                    : null,
                completedSets.Count,
                completedSets.Sum(x => x.Repeticiones),
                calculated.MejorSerie);
        }

        private static DateTime GetUtcStartOfDay(
            DateOnly date,
            TimeZoneInfo timeZone)
        {
            var localTime = DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Unspecified);

            for (var minute = 0;
                minute < 48 * 60 && timeZone.IsInvalidTime(localTime);
                minute++)
            {
                localTime = localTime.AddMinutes(1);
            }

            if (timeZone.IsInvalidTime(localTime))
            {
                throw new InvalidTimeZoneException(
                    "No se pudo determinar el inicio del día en la zona horaria solicitada.");
            }

            if (timeZone.IsAmbiguousTime(localTime))
            {
                return timeZone
                    .GetAmbiguousTimeOffsets(localTime)
                    .Select(offset => new DateTimeOffset(localTime, offset).UtcDateTime)
                    .Min();
            }

            return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZone);
        }
    }
}
