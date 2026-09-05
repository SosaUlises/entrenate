using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Common.Interfaces
{
    public interface IEquipmentRepository
    {
        Task<IReadOnlyCollection<Equipamiento>> GetActiveAsync(
            CancellationToken cancellationToken = default);
    }
}
