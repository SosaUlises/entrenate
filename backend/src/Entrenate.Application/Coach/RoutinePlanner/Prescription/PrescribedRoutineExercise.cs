using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Prescription
{
    public sealed record PrescribedRoutineExercise(
        Guid ExerciseId,
        string Code,
        string Name,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string> SecondaryMuscleGroups,
        PatronMovimiento MovementPattern,
        TipoEjercicio ExerciseType,
        ExerciseSlotRole SlotRole,
        bool IsRequired,
        int Sets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds,
        int Order);
}
