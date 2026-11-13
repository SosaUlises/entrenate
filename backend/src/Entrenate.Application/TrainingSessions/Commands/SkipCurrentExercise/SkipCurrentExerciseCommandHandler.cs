using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.SkipCurrentExercise
{
    public sealed class SkipCurrentExerciseCommandHandler
        : IRequestHandler<SkipCurrentExerciseCommand, TrainingSessionDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;
        private readonly ISender _sender;

        public SkipCurrentExerciseCommandHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository,
            ISender sender)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
            _sender = sender;
        }

        public async Task<TrainingSessionDto> Handle(
            SkipCurrentExerciseCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");

            var session = await _sessionRepository.GetByIdAndUserIdAsync(
                request.SessionId, userId, cancellationToken);
            if (session is null)
                throw new NotFoundException("La sesión de entrenamiento no existe.");
            if (!session.EstaEnCurso())
                throw new ConflictException("La sesión de entrenamiento ya fue finalizada.");

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
                throw new ConflictException("No hay un ejercicio pendiente para omitir.");
            if ((request.ExpectedExerciseSessionId.HasValue &&
                 current.ExerciseSession.Id != request.ExpectedExerciseSessionId.Value) ||
                (request.ExpectedSetNumber.HasValue &&
                 current.SetNumber != request.ExpectedSetNumber.Value))
                throw new ConflictException("La sesión cambió.");

            session.OmitirEjercicio(current.ExerciseSession.Id);
            await _sessionRepository.SaveChangesAsync(cancellationToken);
            return await _sender.Send(new GetTrainingSessionByIdQuery(session.Id), cancellationToken);
        }
    }
}
