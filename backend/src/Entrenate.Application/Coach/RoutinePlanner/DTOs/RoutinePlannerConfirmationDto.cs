namespace Entrenate.Application.Coach.RoutinePlanner.DTOs
{
    public sealed record RoutinePlannerConfirmationDto(
        Guid RoutineId,
        Guid ProposalId,
        bool IsActive,
        bool CreatedNow);
}
