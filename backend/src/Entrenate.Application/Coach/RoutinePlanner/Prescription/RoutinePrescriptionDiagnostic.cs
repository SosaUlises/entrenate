using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Prescription
{
    public sealed record RoutinePrescriptionDiagnostic(
        RoutinePrescriptionDiagnosticCode Code,
        int? DayOrder = null,
        Guid? ExerciseId = null,
        PatronMovimiento? MovementPattern = null,
        string? MuscleGroup = null);
}
