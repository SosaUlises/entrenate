namespace Entrenate.Application.Routines.Models
{
    public record RoutineDayInput(
        string Nombre,
        string? Descripcion,
        int Orden,
        List<RoutineExerciseInput> Ejercicios);
}
