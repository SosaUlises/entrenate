namespace Entrenate.Application.Routines.DTOs
{
    public record RoutineExerciseDto(
        Guid EjercicioId,
        string Nombre,
        int CantidadSeries,
        int RepeticionesMinimas,
        int RepeticionesMaximas,
        int RirObjetivoMinimo,
        int RirObjetivoMaximo,
        int DescansoSegundos,
        int Orden,
        string? Notas);
}
