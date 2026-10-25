using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record RoutineSessionExerciseSelection(
        int DayOrder,
        DiaSemana ScheduledDay,
        RoutineSessionFocus Focus,
        RoutineSessionVariant Variant,
        IReadOnlyList<RoutineExerciseSlotSelection> Slots);
}
