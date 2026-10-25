using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record SelectedRoutineExercise(
        Guid ExerciseId,
        string Code,
        string Name,
        string PrimaryMuscleGroup,
        IReadOnlyCollection<string> SecondaryMuscleGroups,
        PatronMovimiento MovementPattern,
        TipoEjercicio ExerciseType);
}
