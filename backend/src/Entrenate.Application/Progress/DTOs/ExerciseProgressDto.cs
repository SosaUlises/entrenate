namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressDto(
        Guid ExerciseId,
        string Nombre,
        IReadOnlyCollection<ExerciseProgressEntryDto> Entrenamientos,
        ExerciseProgressSummaryDto Resumen);
}
