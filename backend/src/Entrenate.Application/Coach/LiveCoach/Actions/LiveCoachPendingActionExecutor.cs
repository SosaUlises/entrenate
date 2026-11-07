using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Rules;
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
using Entrenate.Application.TrainingSessions.Commands.SkipCurrentExercise;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Actions
{
    public interface ILiveCoachPendingActionExecutor
    {
        Task<LiveCoachActionConfirmationDto> ConfirmAsync(
            Guid actionId,
            bool restTimerIsActive,
            CancellationToken cancellationToken);
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

        public async Task<LiveCoachActionConfirmationDto> ConfirmAsync(
            Guid actionId,
            bool restTimerIsActive,
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
            if (action.Type == LiveCoachActionTypes.SkipCurrentExercise)
            {
                var skippedSession = await _sender.Send(
                    new SkipCurrentExerciseCommand(
                        action.SessionId,
                        action.ExerciseSessionId,
                        action.SetNumber),
                    cancellationToken);
                return new LiveCoachActionConfirmationDto(
                    skippedSession,
                    "Ejercicio omitido. Seguimos con el siguiente.");
            }

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

            var updatedSession = await _sender.Send(
                new GetTrainingSessionByIdQuery(action.SessionId),
                cancellationToken);

            var restEligibility = LiveCoachRestEligibilityResolver.ResolveAfterConfirmedSet(
                updatedSession,
                action.ExerciseSessionId);

            if (restEligibility.Status == LiveCoachRestEligibilityStatus.NoPendingSet)
            {
                return new LiveCoachActionConfirmationDto(
                    updatedSession,
                    "Serie registrada. Completaste todas las series.");
            }

            if (!restTimerIsActive &&
                restEligibility.Status == LiveCoachRestEligibilityStatus.Eligible &&
                restEligibility.RestSeconds is > 0)
            {
                var durationSeconds = restEligibility.RestSeconds.Value;
                return new LiveCoachActionConfirmationDto(
                    updatedSession,
                    $"Serie registrada. Descanso: {durationSeconds} segundos.",
                    new LiveCoachClientActionDto("StartRestTimer", durationSeconds));
            }

            return new LiveCoachActionConfirmationDto(updatedSession, "Serie registrada.");
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
