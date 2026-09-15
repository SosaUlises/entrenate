namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressSummaryDto(
        decimal? PrimerE1Rm,
        decimal? UltimoE1Rm,
        decimal? MejorE1Rm,
        decimal? CambioAbsoluto,
        decimal? CambioPorcentual);
}
