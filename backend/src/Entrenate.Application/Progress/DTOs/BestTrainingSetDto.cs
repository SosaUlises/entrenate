namespace Entrenate.Application.Progress.DTOs
{
    public record BestTrainingSetDto(
        int NumeroSerie,
        decimal Peso,
        int Repeticiones,
        int? Rir);
}
