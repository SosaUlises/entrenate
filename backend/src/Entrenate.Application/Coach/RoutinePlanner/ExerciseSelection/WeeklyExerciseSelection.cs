namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record WeeklyExerciseSelection(
        ExerciseSelectionFeasibility Feasibility,
        IReadOnlyList<RoutineSessionExerciseSelection> Sessions,
        IReadOnlyList<ExerciseSelectionDiagnostic> Diagnostics);
}
