using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Queries.GetActiveTrainingSession
{
    public class GetActiveTrainingSessionQueryHandler
        : IRequestHandler<GetActiveTrainingSessionQuery, TrainingSessionDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetActiveTrainingSessionQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
        }

        public async Task<TrainingSessionDto> Handle(
            GetActiveTrainingSessionQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var session = await _sessionRepository
                .GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);

            if (session is null)
            {
                throw new NotFoundException(
                    "No existe una sesión de entrenamiento en curso.");
            }

            return MapToDto(session);
        }

        private static TrainingSessionDto MapToDto(
            SesionEntrenamiento session)
        {
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
                        x.Ejercicio.Nombre,
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
