using Entrenate.Domain.Enums;

namespace Entrenate.Application.TrainingProfiles.DTOs
{
    public record TrainingProfileEquipmentDto(
     Guid Id,
     string Nombre,
     CategoriaEquipamiento Categoria
 );
}
