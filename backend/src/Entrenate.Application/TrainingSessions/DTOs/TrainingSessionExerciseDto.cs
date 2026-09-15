namespace Entrenate.Application.TrainingSessions.DTOs
{
    public record TrainingSessionExerciseDto(
        Guid Id,
        Guid EjercicioId,
        string Nombre,
        int Orden,
        int SeriesObjetivo,
        int RepeticionesMinimasObjetivo,
        int RepeticionesMaximasObjetivo,
        int RirObjetivoMinimo,
        int RirObjetivoMaximo,
        int DescansoObjetivoSegundos,
        string? Notas,
        IReadOnlyCollection<TrainingSetDto> Series);
}
