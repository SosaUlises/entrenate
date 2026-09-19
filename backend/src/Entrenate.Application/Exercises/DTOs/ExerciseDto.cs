namespace Entrenate.Application.Exercises.DTOs
{
    public record ExerciseDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        string GrupoMuscularPrincipal,
        IReadOnlyCollection<ExerciseEquipmentDto> Equipamientos,
        string? WorkoutGuideId);
}
