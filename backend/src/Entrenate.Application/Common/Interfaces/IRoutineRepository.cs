using Entrenate.Domain.Entidades;
using Entrenate.Application.Coach.RoutinePlanner.Confirmation;

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

        Task<DiaRutina?> GetDayByIdAndUserIdAsync(
            Guid dayId,
            string userId,
            CancellationToken cancellationToken = default);

        Task<bool> HasActiveSessionAsync(
            Guid routineId,
            string userId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            Rutina rutina,
            CancellationToken cancellationToken = default);

        Task<RoutineProposalPersistenceResult> ConfirmPlannerProposalAsync(
            Rutina routine,
            CancellationToken cancellationToken = default);

        void Remove(Rutina rutina);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
