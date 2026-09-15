namespace Entrenate.Application.Routines.DTOs
{
    public record RoutineDayDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        int Orden,
        IReadOnlyCollection<RoutineExerciseDto> Ejercicios);
}
