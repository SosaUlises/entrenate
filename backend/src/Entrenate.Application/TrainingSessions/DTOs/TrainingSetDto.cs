namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingSetDto(
        Guid Id,
        int NumeroSerie,
        decimal Peso,
        int Repeticiones,
        int? Rir,
        bool Completada,
        DateTime FechaHoraRegistro,
        int? RepeticionesObjetivoMinimas = null,
        int? RepeticionesObjetivoMaximas = null,
        int? RirObjetivoMinimo = null,
        int? RirObjetivoMaximo = null);
}
