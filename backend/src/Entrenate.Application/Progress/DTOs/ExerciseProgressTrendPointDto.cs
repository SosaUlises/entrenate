namespace Entrenate.Application.Progress.DTOs
{
    public record ExerciseProgressTrendPointDto(
        Guid SessionId,
        DateTime HoraInicio,
        decimal? E1RmEstimado,
        decimal? PesoMaximo,
        int RepeticionesMaximas,
        decimal? VolumenSesion,
        decimal? VolumenMaximoSerie,
        int SeriesCompletadas,
        int RepeticionesTotales,
        BestTrainingSetDto? MejorSerie);
}
