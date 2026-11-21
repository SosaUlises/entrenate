using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Adaptation
{
    public sealed class ApplyLivePrescriptionAdaptationCommandHandler
        : IRequestHandler<ApplyLivePrescriptionAdaptationCommand, TrainingSessionDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;
        private readonly ISender _sender;

        public ApplyLivePrescriptionAdaptationCommandHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository,
            ISender sender)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
            _sender = sender;
        }

        public async Task<TrainingSessionDto> Handle(
            ApplyLivePrescriptionAdaptationCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");
            if (!string.Equals(request.Proposal.UserId, userId, StringComparison.Ordinal))
                throw new NotFoundException("La propuesta de ajuste no existe.");
            if (request.Proposal.ExpiresAtUtc <= DateTime.UtcNow)
                throw new ConflictException("La propuesta de ajuste expiro.");

            var session = await _sessionRepository.GetByIdAndUserIdAsync(
                request.Proposal.SessionId,
                userId,
                cancellationToken);
            if (session is null)
                throw new NotFoundException("La sesion de entrenamiento no existe.");
            if (!session.EstaEnCurso())
                throw new ConflictException("La sesion de entrenamiento ya fue finalizada.");
            if (!string.Equals(
                request.Proposal.PendingWorkFingerprint,
                LiveTimeConstraintFingerprint.Create(session),
                StringComparison.Ordinal))
            {
                throw new ConflictException("La sesion cambio. Pedi un ajuste nuevo.");
            }

            session.AdaptarPrescripcionPendiente(
                request.Proposal.ExerciseSessionId,
                request.Proposal.EffectiveFromSetNumber,
                request.Proposal.PreviousRepMin,
                request.Proposal.PreviousRepMax,
                request.Proposal.PreviousRirMin,
                request.Proposal.PreviousRirMax,
                request.Proposal.NewRepMin,
                request.Proposal.NewRepMax,
                request.Proposal.NewRirMin,
                request.Proposal.NewRirMax);

            await _sessionRepository.SaveChangesAsync(cancellationToken);
            return await _sender.Send(new GetTrainingSessionByIdQuery(session.Id), cancellationToken);
        }
    }
}
