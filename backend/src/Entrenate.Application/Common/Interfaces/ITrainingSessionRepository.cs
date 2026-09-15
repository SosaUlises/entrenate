using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ITrainingSessionRepository
    {
        Task<bool> HasActiveSessionAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            SesionEntrenamiento session,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
