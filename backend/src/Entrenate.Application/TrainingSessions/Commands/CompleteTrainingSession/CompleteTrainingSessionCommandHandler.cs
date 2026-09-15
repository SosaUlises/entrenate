using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.CompleteTrainingSession
{
    public class CompleteTrainingSessionCommandHandler
        : IRequestHandler<CompleteTrainingSessionCommand>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;

        public CompleteTrainingSessionCommandHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
        }

        public async Task Handle(
            CompleteTrainingSessionCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var session = await _sessionRepository.GetByIdAndUserIdAsync(
                request.SessionId,
                userId,
                cancellationToken);

            if (session is null)
            {
                throw new NotFoundException(
                    "La sesión de entrenamiento no existe.");
            }

            if (!session.EstaEnCurso())
            {
                throw new ConflictException(
                    "La sesión de entrenamiento ya fue finalizada.");
            }

            session.Completar(DateTime.UtcNow);

            await _sessionRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
