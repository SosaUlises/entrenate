using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    public interface ILiveCoachPendingActionExecutor
    {
        Task<TrainingSessionDto> ConfirmAsync(Guid actionId, CancellationToken cancellationToken);
        Task CancelAsync(Guid actionId, CancellationToken cancellationToken);
    }

    public sealed class LiveCoachPendingActionExecutor : ILiveCoachPendingActionExecutor
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ILiveCoachPendingActionStore _actionStore;
        private readonly ISender _sender;

        public LiveCoachPendingActionExecutor(
            ICurrentUserService currentUserService,
            ILiveCoachPendingActionStore actionStore,
            ISender sender)
        {
            _currentUserService = currentUserService;
            _actionStore = actionStore;
            _sender = sender;
        }

        public async Task<TrainingSessionDto> ConfirmAsync(
            Guid actionId,
            CancellationToken cancellationToken)
        {
            var userId = GetAuthenticatedUserId();
            var take = _actionStore.Take(actionId, userId, DateTime.UtcNow);

            if (take.Status == PendingLiveCoachActionTakeStatus.NotOwned)
                throw new NotFoundException("La acción pendiente no existe.");
            if (take.Status == PendingLiveCoachActionTakeStatus.Expired)
                throw new ConflictException("La acción expiró. Volvé a indicar la serie.");
            if (take.Status != PendingLiveCoachActionTakeStatus.Success || take.Action is null)
                throw new ConflictException("La acción ya no está disponible.");

            var action = take.Action;
            await _sender.Send(
                new UpsertTrainingSetCommand(
                    action.SessionId,
                    action.ExerciseSessionId,
                    action.SetNumber,
                    action.WeightKg,
                    action.Repetitions,
                    action.Rir,
                    RequireCurrentPendingSet: true),
                cancellationToken);

            return await _sender.Send(
                new GetTrainingSessionByIdQuery(action.SessionId),
                cancellationToken);
        }

        public Task CancelAsync(Guid actionId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var userId = GetAuthenticatedUserId();
            var result = _actionStore.Cancel(actionId, userId, DateTime.UtcNow);
            if (result.Status == PendingLiveCoachActionTakeStatus.NotOwned)
                throw new NotFoundException("La acción pendiente no existe.");

            return Task.CompletedTask;
        }

        private string GetAuthenticatedUserId()
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            return userId;
        }
    }
}
