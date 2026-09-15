namespace Entrenate.Application.Routines.Models
{
    public record RoutineExerciseInput(
        Guid EjercicioId,
        int CantidadSeries,
        int RepeticionesMinimas,
        int RepeticionesMaximas,
        int RirObjetivoMinimo,
        int RirObjetivoMaximo,
        int DescansoSegundos,
        int Orden,
        string? Notas);
}
