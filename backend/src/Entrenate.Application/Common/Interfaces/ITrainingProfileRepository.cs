using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ITrainingProfileRepository
    {
        Task<PerfilEntrenamiento?> GetByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            PerfilEntrenamiento perfil,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
