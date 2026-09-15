using Entrenate.Domain.Enums;

namespace Entrenate.Application.Exercises.DTOs
{
    public record ExerciseEquipmentDto(
        Guid Id,
        string Nombre,
        CategoriaEquipamiento Categoria);
}
