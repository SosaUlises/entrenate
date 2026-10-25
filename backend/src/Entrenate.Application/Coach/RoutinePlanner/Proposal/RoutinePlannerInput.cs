using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Proposal
{
    public sealed record RoutinePlannerInput(
        string PolicyVersion,
        ObjetivoEntrenamiento Goal,
        NivelExperiencia ExperienceLevel,
        int DaysPerWeek,
        IReadOnlyCollection<DiaSemana> PreferredDays,
        int TargetSessionDurationMinutes,
        IReadOnlyCollection<Guid> AvailableEquipmentIds,
        IReadOnlyCollection<PlannerExerciseCandidate> Catalog);
}
