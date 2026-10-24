namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingActivityDayDto(
        DateOnly Date,
        int SessionCount);
}
