using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Commands.CancelLiveCoachAction
{
    public sealed class CancelLiveCoachActionCommandHandler
        : IRequestHandler<CancelLiveCoachActionCommand>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ILiveCoachPendingActionStore _actionStore;

        public CancelLiveCoachActionCommandHandler(
            ICurrentUserService currentUserService,
            ILiveCoachPendingActionStore actionStore)
        {
            _currentUserService = currentUserService;
            _actionStore = actionStore;
        }

        public Task Handle(CancelLiveCoachActionCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            var result = _actionStore.Cancel(request.ActionId, userId, DateTime.UtcNow);
            if (result.Status == PendingLiveCoachActionTakeStatus.NotOwned)
                throw new NotFoundException("La acción pendiente no existe.");

            return Task.CompletedTask;
        }
    }
}
