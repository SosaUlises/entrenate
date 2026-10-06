using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetTrainingActivity
{
    public class GetTrainingActivityQueryHandler
        : IRequestHandler<GetTrainingActivityQuery, TrainingActivityDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetTrainingActivityQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
        }

        public async Task<TrainingActivityDto> Handle(
            GetTrainingActivityQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var from = request.From!.Value;
            var to = request.To!.Value;
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone!);
            var fromUtc = GetUtcStartOfDay(from, timeZone);
            var toUtcExclusive = GetUtcStartOfDay(to.AddDays(1), timeZone);

            var sessionStartTimes = await _sessionRepository
                .GetCompletedSessionStartTimesByUserIdAsync(
                    userId,
                    fromUtc,
                    toUtcExclusive,
                    cancellationToken);

            var days = sessionStartTimes
                .Select(startTime => TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(startTime, DateTimeKind.Utc),
                    timeZone))
                .Select(DateOnly.FromDateTime)
                .Where(date => date >= from && date <= to)
                .GroupBy(date => date)
                .OrderBy(group => group.Key)
                .Select(group => new TrainingActivityDayDto(
                    group.Key,
                    group.Count()))
                .ToList();

            return new TrainingActivityDto(
                from,
                to,
                days.Sum(day => day.SessionCount),
                days.Count,
                days);
        }

        private static DateTime GetUtcStartOfDay(
            DateOnly date,
            TimeZoneInfo timeZone)
        {
            var localTime = DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Unspecified);

            // Some zones skip local midnight during a DST transition.
            // Advance to the first real instant belonging to that date.
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
