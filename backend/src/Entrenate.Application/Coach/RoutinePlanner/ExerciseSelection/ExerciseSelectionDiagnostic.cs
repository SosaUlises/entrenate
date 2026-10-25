namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record ExerciseSelectionDiagnostic(
        ExerciseSelectionDiagnosticCode Code,
        int DayOrder,
        int SlotPosition);
}
