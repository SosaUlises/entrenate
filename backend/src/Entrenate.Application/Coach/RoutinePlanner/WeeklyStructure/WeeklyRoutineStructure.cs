namespace Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure
{
    public sealed record WeeklyRoutineStructure(
        int DaysPerWeek,
        IReadOnlyList<WeeklyRoutineDay> Days);
}
