namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingSetDto(
        Guid Id,
        int NumeroSerie,
        decimal Peso,
        int Repeticiones,
        int? Rir,
        bool Completada,
        DateTime FechaHoraRegistro);
}
