using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction
{
    public sealed class ConfirmLiveCoachActionCommandHandler
        : IRequestHandler<ConfirmLiveCoachActionCommand, TrainingSessionDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ILiveCoachPendingActionStore _actionStore;
        private readonly ISender _sender;

        public ConfirmLiveCoachActionCommandHandler(
            ICurrentUserService currentUserService,
            ILiveCoachPendingActionStore actionStore,
            ISender sender)
        {
            _currentUserService = currentUserService;
            _actionStore = actionStore;
            _sender = sender;
        }

        public async Task<TrainingSessionDto> Handle(
            ConfirmLiveCoachActionCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            var take = _actionStore.Take(request.ActionId, userId, DateTime.UtcNow);
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
    }
}
