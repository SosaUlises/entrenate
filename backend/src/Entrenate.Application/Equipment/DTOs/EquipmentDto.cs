using Entrenate.Domain.Enums;

namespace Entrenate.Application.Equipment.DTOs
{
    public record EquipmentDto(
     Guid Id,
     string Nombre,
     CategoriaEquipamiento Categoria
 );
}
