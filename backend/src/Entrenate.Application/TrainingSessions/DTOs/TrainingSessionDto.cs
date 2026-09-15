using Entrenate.Domain.Enums;

namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingSessionDto(
        Guid Id,
        Guid? DiaRutinaId,
        DateTime Fecha,
        DateTime HoraInicio,
        EstadoSesionEntrenamiento Estado,
        IReadOnlyCollection<TrainingSessionExerciseDto> Ejercicios);
}
