using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record PlannerExerciseCandidate(
        Guid ExerciseId,
        string? Code,
        string Name,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string> SecondaryMuscleGroups,
        PatronMovimiento? MovementPattern,
        TipoEjercicio? ExerciseType,
        bool IsActive,
        IReadOnlyCollection<ExerciseEquipmentRequirement>
            EquipmentRequirements);
}
