namespace Entrenate.Application.Coach.RoutinePlanner.Confirmation
{
    public sealed record RoutineProposalPersistenceResult(
        Guid RoutineId,
        bool IsActive,
        bool CreatedNow);
}
