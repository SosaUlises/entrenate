namespace Entrenate.Api.Requests.Coach
{
    public sealed record GenerateSessionAdaptationProposalRequest(
        Guid RoutineDayId,
        int? AvailableMinutes,
        IReadOnlyCollection<Guid>? AvailableEquipmentIds,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds,
        string? EffortPreference);
}

