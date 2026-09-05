using Entrenate.Domain.Enums;

namespace Entrenate.Application.TrainingProfiles.DTOs
{
    public record TrainingProfileDto(
      Guid Id,
      ObjetivoEntrenamiento Objetivo,
      NivelExperiencia NivelExperiencia,
      int Edad,
      Sexo? Sexo,
      decimal? PesoKg,
      int DiasEntrenamientoPorSemana,
      int DuracionSesionMinutos,
      EntornoEntrenamiento EntornoEntrenamiento,
      IReadOnlyCollection<DiaSemana> DiasPreferidos,
      IReadOnlyCollection<TrainingProfileEquipmentDto> Equipamientos,
      DateTime CreadoEnUtc,
      DateTime ActualizadoEnUtc
  );
}
