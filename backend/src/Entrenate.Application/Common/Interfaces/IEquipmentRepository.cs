using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Common.Interfaces
{
    public interface IEquipmentRepository
    {
        Task<IReadOnlyCollection<Equipamiento>> GetActiveAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Equipamiento>> GetByIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<Guid>> GetValidActiveIdsAsync(
             IEnumerable<Guid> ids,
             CancellationToken cancellationToken = default);
    }
}
