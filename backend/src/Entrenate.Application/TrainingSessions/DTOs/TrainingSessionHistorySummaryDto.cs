using Entrenate.Domain.Enums;

namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingSessionHistorySummaryDto(
        Guid Id,
        Guid? DiaRutinaId,
        DateTime Fecha,
        DateTime HoraInicio,
        DateTime? HoraFin,
        EstadoSesionEntrenamiento Estado,
        int CantidadEjercicios,
        int CantidadSeriesCompletadas);
}
