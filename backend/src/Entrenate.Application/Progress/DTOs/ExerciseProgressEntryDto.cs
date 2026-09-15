using Entrenate.Application.TrainingSessions.DTOs;

namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressEntryDto(
        Guid SessionId,
        DateTime Fecha,
        DateTime HoraInicio,
        IReadOnlyCollection<TrainingSetDto> Series);
}
