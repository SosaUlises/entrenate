using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    public sealed record RoutinePlannerProposal(
        Guid ProposalId,
        string PolicyVersion,
        string CanonicalHash,
        RoutineProposalFeasibility Feasibility,
        ObjetivoEntrenamiento Goal,
        NivelExperiencia ExperienceLevel,
        int RequestedDaysPerWeek,
        IReadOnlyList<RoutinePlannerProposalDay> Days,
        IReadOnlyList<WeeklyMuscleVolume> WeeklyMuscleVolumes,
        IReadOnlyList<ExerciseSelectionDiagnostic> SelectionDiagnostics,
        IReadOnlyList<RoutinePrescriptionDiagnostic>
            PrescriptionDiagnostics);
}
