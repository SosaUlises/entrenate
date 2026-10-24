namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressTrendDto(
        Guid ExerciseId,
        string Nombre,
        bool TieneHistorial,
        IReadOnlyCollection<ExerciseProgressTrendPointDto> Points);
}
