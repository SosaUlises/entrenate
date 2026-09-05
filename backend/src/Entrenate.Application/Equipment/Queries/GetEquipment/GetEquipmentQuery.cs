using Entrenate.Application.Equipment.DTOs;
using MediatR;

namespace Entrenate.Application.Equipment.Queries.GetEquipment
{
    public record GetEquipmentQuery
    : IRequest<IReadOnlyCollection<EquipmentDto>>;
}
