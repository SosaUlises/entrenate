using Entrenate.Application.TrainingSessions.DTOs;

namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressEntryDto(
        Guid SessionId,
        DateTime Fecha,
        DateTime HoraInicio,
        IReadOnlyCollection<TrainingSetDto> Series,
        decimal? VolumenTotal,
        BestTrainingSetDto? MejorSerie,
        decimal? E1RmEstimado)
    {
        public ExerciseProgressEntryDto(
            Guid sessionId,
            DateTime fecha,
            DateTime horaInicio,
            IReadOnlyCollection<TrainingSetDto> series)
            : this(
                sessionId,
                fecha,
                horaInicio,
                series,
                null,
                null,
                null)
        {
        }
    }
}
