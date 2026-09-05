using Entrenate.Domain.Enums;
using MediatR;

namespace Entrenate.Application.TrainingProfiles.Commands.UpdateTrainingProfile
{
    public record UpdateTrainingProfileCommand(
     ObjetivoEntrenamiento Objetivo,
     NivelExperiencia NivelExperiencia,
     int Edad,
     Sexo? Sexo,
     decimal? PesoKg,
     int DiasEntrenamientoPorSemana,
     int DuracionSesionMinutos,
     EntornoEntrenamiento EntornoEntrenamiento,
     List<DiaSemana> DiasPreferidos,
     List<Guid> EquipamientoIds
 ) : IRequest;
}
