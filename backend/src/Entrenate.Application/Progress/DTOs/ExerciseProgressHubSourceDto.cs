using Entrenate.Application.TrainingSessions.DTOs;

namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressHubSourceDto(
        Guid ExerciseId,
        string Nombre,
        string GrupoMuscularPrincipal,
        Guid SessionId,
        DateTime Fecha,
        DateTime HoraInicio,
        IReadOnlyCollection<TrainingSetDto> Series);
}
