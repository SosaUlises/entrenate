using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;

namespace Entrenate.Application.Coach.RoutinePlanner.Prescription
{
    public sealed record WeeklyRoutinePrescription(
        RoutinePrescriptionFeasibility Feasibility,
        IReadOnlyList<PrescribedRoutineSession> Sessions,
        IReadOnlyList<WeeklyMuscleVolume> WeeklyMuscleVolumes,
        IReadOnlyList<ExerciseSelectionDiagnostic> SelectionDiagnostics,
        IReadOnlyList<RoutinePrescriptionDiagnostic>
            PrescriptionDiagnostics);
}
