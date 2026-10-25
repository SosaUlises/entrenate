using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure
{
    public sealed record WeeklyRoutineDay(
        int Order,
        DiaSemana ScheduledDay,
        ScheduledDayOrigin DayOrigin,
        RoutineSessionFocus Focus,
        RoutineSessionVariant Variant);
}
