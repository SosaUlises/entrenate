using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Common.Interfaces
{
    public interface IRoutineRepository
    {
        Task<IReadOnlyCollection<Rutina>> GetByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<Rutina?> GetDetailByIdAndUserIdAsync(
            Guid id,
            string userId,
            CancellationToken cancellationToken = default);

        Task<Rutina?> GetForUpdateByIdAndUserIdAsync(
            Guid id,
            string userId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Rutina rutina,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
