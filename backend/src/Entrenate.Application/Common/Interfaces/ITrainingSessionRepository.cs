using Entrenate.Domain.Entidades;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;

namespace Entrenate.Application.Common.Interfaces
{
    public interface ITrainingSessionRepository
    {
        Task<bool> HasActiveSessionAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<SesionEntrenamiento?> GetActiveByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(
            Guid sessionId,
            string userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>>
            GetHistoryByUserIdAsync(
                string userId,
                CancellationToken cancellationToken = default);

        Task<SesionEntrenamiento?> GetByIdAndUserIdAsNoTrackingAsync(
            Guid sessionId,
            string userId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ExerciseProgressEntryDto>>
            GetCompletedExerciseProgressByUserIdAsync(
                Guid exerciseId,
                string userId,
                CancellationToken cancellationToken = default);

        Task AddAsync(
            SesionEntrenamiento session,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
