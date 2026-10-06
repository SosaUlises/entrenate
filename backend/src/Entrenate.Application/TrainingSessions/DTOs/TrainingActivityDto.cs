namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingActivityDto(
        DateOnly From,
        DateOnly To,
        int TotalSessions,
        int ActiveDays,
        IReadOnlyCollection<TrainingActivityDayDto> Days);
}
