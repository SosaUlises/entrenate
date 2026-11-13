using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed class ApplyLiveAdaptationCommandHandler
        : IRequestHandler<ApplyLiveAdaptationCommand, TrainingSessionDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;
        private readonly ISender _sender;

        public ApplyLiveAdaptationCommandHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository,
            ISender sender)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
            _sender = sender;
        }

        public async Task<TrainingSessionDto> Handle(
            ApplyLiveAdaptationCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");
            if (!string.Equals(request.Proposal.UserId, userId, StringComparison.Ordinal))
                throw new NotFoundException("La propuesta de ajuste no existe.");

            var session = await _sessionRepository.GetByIdAndUserIdAsync(
                request.Proposal.SessionId,
                userId,
                cancellationToken);
            if (session is null)
                throw new NotFoundException("La sesión de entrenamiento no existe.");
            if (!session.EstaEnCurso())
                throw new ConflictException("La sesión de entrenamiento ya fue finalizada.");
            if (!string.Equals(
                request.Proposal.PendingWorkFingerprint,
                LiveTimeConstraintFingerprint.Create(session),
                StringComparison.Ordinal))
            {
                throw new ConflictException(
                    "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo.");
            }

            foreach (var change in request.Proposal.Changes)
            {
                if (change.Type != LiveAdaptationChangeType.OmitExercise)
                    throw new ConflictException("La propuesta contiene un cambio no compatible.");

                session.OmitirEjercicio(change.ExerciseSessionId);
            }

            await _sessionRepository.SaveChangesAsync(cancellationToken);
            return await _sender.Send(
                new GetTrainingSessionByIdQuery(session.Id),
                cancellationToken);
        }
    }
}
