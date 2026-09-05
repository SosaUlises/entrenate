using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Equipment.DTOs;
using MediatR;

namespace Entrenate.Application.Equipment.Queries.GetEquipment
{
    public class GetEquipmentQueryHandler
      : IRequestHandler<
          GetEquipmentQuery,
          IReadOnlyCollection<EquipmentDto>>
    {
        private readonly IEquipmentRepository _repository;

        public GetEquipmentQueryHandler(
            IEquipmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyCollection<EquipmentDto>> Handle(
            GetEquipmentQuery request,
            CancellationToken cancellationToken)
        {
            var equipment = await _repository.GetActiveAsync(
                cancellationToken);

            return equipment
                .Select(x => new EquipmentDto(
                    x.Id,
                    x.Nombre,
                    x.Categoria))
                .ToList();
        }
    }
}
