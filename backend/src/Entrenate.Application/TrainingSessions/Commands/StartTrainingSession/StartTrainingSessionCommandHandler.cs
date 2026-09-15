using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.StartTrainingSession
{
    public class StartTrainingSessionCommandHandler
        : IRequestHandler<StartTrainingSessionCommand, TrainingSessionDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRoutineRepository _routineRepository;
        private readonly ITrainingSessionRepository _sessionRepository;

        public StartTrainingSessionCommandHandler(
            ICurrentUserService currentUserService,
            IRoutineRepository routineRepository,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _routineRepository = routineRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<TrainingSessionDto> Handle(
            StartTrainingSessionCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var routineDay = await _routineRepository
                .GetDayByIdAndUserIdAsync(
                    request.DiaRutinaId,
                    userId,
                    cancellationToken);

            if (routineDay is null)
            {
                throw new NotFoundException(
                    "El día de rutina no existe.");
            }

            if (await _sessionRepository.HasActiveSessionAsync(
                userId,
                cancellationToken))
            {
                throw new ConflictException(
                    "Ya existe una sesión de entrenamiento en curso.");
            }

            var session = new SesionEntrenamiento(
                userId,
                routineDay.Id,
                DateTime.UtcNow,
                routineDay.Ejercicios);

            await _sessionRepository.AddAsync(
                session,
                cancellationToken);

            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var exerciseNamesByOrder = routineDay.Ejercicios
                .ToDictionary(x => x.Orden, x => x.Ejercicio.Nombre);

            return new TrainingSessionDto(
                session.Id,
                session.DiaRutinaId,
                session.Fecha,
                session.HoraInicio,
                session.HoraFin,
                session.Estado,
                session.Ejercicios
                    .OrderBy(x => x.Orden)
                    .Select(x => new TrainingSessionExerciseDto(
                        x.Id,
                        x.EjercicioId,
                        exerciseNamesByOrder[x.Orden],
                        x.Orden,
                        x.SeriesObjetivo,
                        x.RepeticionesMinimasObjetivo,
                        x.RepeticionesMaximasObjetivo,
                        x.RirObjetivoMinimo,
                        x.RirObjetivoMaximo,
                        x.DescansoObjetivoSegundos,
                        x.Notas,
                        x.Series
                            .OrderBy(y => y.NumeroSerie)
                            .Select(y => new TrainingSetDto(
                                y.Id,
                                y.NumeroSerie,
                                y.Peso,
                                y.Repeticiones,
                                y.Rir,
                                y.Completada,
                                y.FechaHoraRegistro))
                            .ToList()))
                    .ToList());
        }
    }
}
