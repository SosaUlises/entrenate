using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Prescription
{
    public sealed record PrescribedRoutineSession(
        int DayOrder,
        DiaSemana ScheduledDay,
        RoutineSessionFocus Focus,
        RoutineSessionVariant Variant,
        int TargetDurationMinutes,
        int EstimatedDurationSeconds,
        IReadOnlyList<PrescribedRoutineExercise> Exercises);
}
