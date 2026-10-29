using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    public sealed record RoutinePlannerProposalExercise(
        Guid ExerciseId,
        string Code,
        string Name,
        int Order,
        PatronMovimiento MovementPattern,
        TipoEjercicio ExerciseType,
        ExerciseSlotRole Role,
        bool IsRequired,
        int Sets,
        int MinReps,
        int MaxReps,
        int MinRir,
        int MaxRir,
        int RestSeconds);
}
