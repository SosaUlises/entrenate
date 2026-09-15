namespace Entrenate.Api.Requests.TrainingSessions
{
    public record UpsertTrainingSetRequest(
        decimal Peso,
        int Repeticiones,
        int? Rir);
}
