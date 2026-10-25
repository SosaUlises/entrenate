using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record RoutineExerciseSlotSelection(
        int Position,
        ExerciseSlotRole Role,
        bool IsRequired,
        IReadOnlyList<PatronMovimiento> AcceptedPatterns,
        TipoEjercicio RequiredExerciseType,
        SelectedRoutineExercise? Exercise);
}
