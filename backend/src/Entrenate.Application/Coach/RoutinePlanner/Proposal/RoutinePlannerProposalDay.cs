using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    public sealed record RoutinePlannerProposalDay(
        int Order,
        DiaSemana ScheduledDay,
        ScheduledDayOrigin DayOrigin,
        RoutineSessionFocus Focus,
        RoutineSessionVariant Variant,
        int TargetDurationMinutes,
        int EstimatedDurationSeconds,
        IReadOnlyList<RoutinePlannerProposalExercise> Exercises);
}
