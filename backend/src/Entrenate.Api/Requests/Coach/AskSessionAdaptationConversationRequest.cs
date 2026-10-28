namespace Entrenate.Api.Requests.Coach
{
    public sealed record AskSessionAdaptationConversationRequest(
        string ProposalHash,
        Guid RoutineDayId,
        int? AvailableMinutes,
        IReadOnlyCollection<Guid>? AvailableEquipmentIds,
        IReadOnlyCollection<Guid>? ExcludedExerciseIds,
        string? EffortPreference,
        string Question);
}

