namespace Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection
{
    public sealed record ExerciseEquipmentRequirement(
        Guid EquipmentId,
        bool IsActive);
}
