using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet
{
    public class UpsertTrainingSetCommandHandler
        : IRequestHandler<UpsertTrainingSetCommand, TrainingSetDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;

        public UpsertTrainingSetCommandHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
        }

        public async Task<TrainingSetDto> Handle(
            UpsertTrainingSetCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var session = await _sessionRepository
                .GetByIdAndUserIdAsync(
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
                    "La sesión de entrenamiento no está en curso.");
            }

            var exerciseSession = session.Ejercicios.FirstOrDefault(
                x => x.Id == request.ExerciseSessionId);

            if (exerciseSession is null)
            {
                throw new NotFoundException(
                    "El ejercicio no pertenece a la sesión de entrenamiento.");
            }

            var trainingSet = exerciseSession.RegistrarOActualizarSerie(
                request.SetNumber,
                request.Peso,
                request.Repeticiones,
                request.Rir,
                DateTime.UtcNow);

            await _sessionRepository.SaveChangesAsync(cancellationToken);

            return new TrainingSetDto(
                trainingSet.Id,
                trainingSet.NumeroSerie,
                trainingSet.Peso,
                trainingSet.Repeticiones,
                trainingSet.Rir,
                trainingSet.Completada,
                trainingSet.FechaHoraRegistro);
        }
    }
}
