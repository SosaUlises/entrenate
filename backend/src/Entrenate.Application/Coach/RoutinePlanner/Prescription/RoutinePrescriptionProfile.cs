using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.Prescription
{
    public sealed record RoutinePrescriptionProfile(
        ObjetivoEntrenamiento Goal,
        NivelExperiencia ExperienceLevel,
        int TargetSessionDurationMinutes);
}
