using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using MediatR;

namespace Entrenate.Application.Progress.Queries.GetExerciseProgressHub
{
    public class GetExerciseProgressHubQueryHandler
        : IRequestHandler<GetExerciseProgressHubQuery, ExerciseProgressHubDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ITrainingSessionRepository _sessionRepository;

        public GetExerciseProgressHubQueryHandler(
            ICurrentUserService currentUserService,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _sessionRepository = sessionRepository;
        }

        public async Task<ExerciseProgressHubDto> Handle(
            GetExerciseProgressHubQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var sourceEntries = await _sessionRepository
                .GetCompletedExerciseProgressHubByUserIdAsync(
                    userId,
                    cancellationToken);

            var exercises = sourceEntries
                .GroupBy(x => new
                {
                    x.ExerciseId,
                    x.Nombre,
                    x.GrupoMuscularPrincipal
                })
                .Select(exerciseGroup => BuildItem(
                    exerciseGroup.Key.ExerciseId,
                    exerciseGroup.Key.Nombre,
                    exerciseGroup.Key.GrupoMuscularPrincipal,
                    exerciseGroup))
                .OrderByDescending(x => x.UltimoEntrenamiento)
                .ThenBy(x => x.Nombre)
                .ThenBy(x => x.EjercicioId)
                .ToList();

            return new ExerciseProgressHubDto(exercises);
        }

        private static ExerciseProgressHubItemDto BuildItem(
            Guid exerciseId,
            string nombre,
            string grupoMuscularPrincipal,
            IEnumerable<ExerciseProgressHubSourceDto> sourceEntries)
        {
            var entries = sourceEntries
                .GroupBy(x => new
                {
                    x.SessionId,
                    x.Fecha,
                    x.HoraInicio
                })
                .Select(sessionGroup => new ExerciseProgressEntryDto(
                    sessionGroup.Key.SessionId,
                    sessionGroup.Key.Fecha,
                    sessionGroup.Key.HoraInicio,
                    sessionGroup
                        .SelectMany(x => x.Series)
                        .OrderBy(x => x.NumeroSerie)
                        .ThenBy(x => x.Id)
                        .ToList()))
                .Select(ExerciseProgressMetricsCalculator.Calculate)
                .OrderBy(x => x.HoraInicio)
                .ThenBy(x => x.SessionId)
                .ToList();

            var latestEntry = entries.Last();
            var summary = ExerciseProgressSummaryCalculator.Calculate(entries);
            var lastRegisteredSet = latestEntry.Series
                .OrderByDescending(x => x.FechaHoraRegistro)
                .ThenByDescending(x => x.NumeroSerie)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            return new ExerciseProgressHubItemDto(
                exerciseId,
                nombre,
                grupoMuscularPrincipal,
                latestEntry.HoraInicio,
                entries.Count,
                latestEntry.MejorSerie,
                lastRegisteredSet is null
                    ? null
                    : new BestTrainingSetDto(
                        lastRegisteredSet.NumeroSerie,
                        lastRegisteredSet.Peso,
                        lastRegisteredSet.Repeticiones,
                        lastRegisteredSet.Rir),
                summary.UltimoE1Rm,
                summary.CambioPorcentual);
        }
    }
}
