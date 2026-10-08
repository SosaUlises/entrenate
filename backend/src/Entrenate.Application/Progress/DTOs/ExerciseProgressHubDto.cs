namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressHubDto(
        IReadOnlyCollection<ExerciseProgressHubItemDto> Ejercicios);

    public record ExerciseProgressHubItemDto(
        Guid EjercicioId,
        string Nombre,
        string GrupoMuscularPrincipal,
        DateTime UltimoEntrenamiento,
        int CantidadSesiones,
        BestTrainingSetDto? MejorSerieUltimaSesion,
        BestTrainingSetDto? UltimaSerieRegistrada,
        decimal? E1RmActual,
        decimal? CambioPorcentual);
}
