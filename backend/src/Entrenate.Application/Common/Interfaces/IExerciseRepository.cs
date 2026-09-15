using Entrenate.Domain.Entidades;

namespace Entrenate.Application.Common.Interfaces
{
    public interface IExerciseRepository
    {
        Task<IReadOnlyCollection<Ejercicio>> GetActiveAsync(
            CancellationToken cancellationToken = default);

        Task<Ejercicio?> GetActiveByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
