using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetActiveTrainingSession;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.TrainingSessions
{
    public sealed class GetActiveTrainingSessionQueryHandlerTests
    {
        private sealed class TestCurrentUserService : ICurrentUserService
        {
            public string? UserId { get; set; }
            public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        }

        private sealed class TestTrainingSessionRepository : ITrainingSessionRepository
        {
            public SesionEntrenamiento? ActiveSession { get; set; }

            public Task<SesionEntrenamiento?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
                => Task.FromResult(ActiveSession?.UsuarioId == userId ? ActiveSession : null);

            public Task AddAsync(SesionEntrenamiento session, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<AdaptiveSessionPersistenceResult> ConfirmAdaptiveSessionAsync(SesionEntrenamiento session, SesionAdaptacion adaptation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsNoTrackingAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>> GetHistoryByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<DateTime>> GetCompletedSessionStartTimesByUserIdAsync(string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressTrendByUserIdAsync(Guid exerciseId, string userId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> HasCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ExerciseProgressHubSourceDto>> GetCompletedExerciseProgressHubByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ProgressExposure>> GetRecentCompletedExerciseExposuresByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
        {
            var userService = new TestCurrentUserService { UserId = null };
            var repo = new TestTrainingSessionRepository();
            var handler = new GetActiveTrainingSessionQueryHandler(userService, repo);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                handler.Handle(new GetActiveTrainingSessionQuery(), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenNoActiveSession_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid().ToString();
            var userService = new TestCurrentUserService { UserId = userId };
            var repo = new TestTrainingSessionRepository { ActiveSession = null };
            var handler = new GetActiveTrainingSessionQueryHandler(userService, repo);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new GetActiveTrainingSessionQuery(), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenActiveSessionHasLegacySetsWithoutPrescriptionSnapshots_MapsCorrectly()
        {
            var userId = Guid.NewGuid().ToString();
            var userService = new TestCurrentUserService { UserId = userId };
            var diaRutinaId = Guid.NewGuid();

            var exercise = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Press banca con barra",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };

            var exerciseSession = new EjercicioSesion(
                exercise.Id,
                orden: 1,
                seriesObjetivo: 4,
                repeticionesMinimasObjetivo: 8,
                repeticionesMaximasObjetivo: 12,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoObjetivoSegundos: 90,
                notas: null);

            typeof(EjercicioSesion)
                .GetProperty(nameof(EjercicioSesion.Ejercicio))!
                .SetValue(exerciseSession, exercise);

            exerciseSession.RegistrarOActualizarSerie(1, 80, 10, 2, DateTime.UtcNow);

            // Simulate legacy series with null prescription snapshots
            var set = exerciseSession.Series.First();
            typeof(SerieEntrenamiento)
                .GetProperty(nameof(SerieEntrenamiento.RepeticionesObjetivoMinimas))!
                .SetValue(set, null);
            typeof(SerieEntrenamiento)
                .GetProperty(nameof(SerieEntrenamiento.RepeticionesObjetivoMaximas))!
                .SetValue(set, null);
            typeof(SerieEntrenamiento)
                .GetProperty(nameof(SerieEntrenamiento.RirObjetivoMinimo))!
                .SetValue(set, null);
            typeof(SerieEntrenamiento)
                .GetProperty(nameof(SerieEntrenamiento.RirObjetivoMaximo))!
                .SetValue(set, null);

            var session = new SesionEntrenamiento(
                userId,
                diaRutinaId,
                DateTime.UtcNow,
                new[] { exerciseSession });

            var repo = new TestTrainingSessionRepository { ActiveSession = session };
            var handler = new GetActiveTrainingSessionQueryHandler(userService, repo);

            var result = await handler.Handle(new GetActiveTrainingSessionQuery(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(session.Id, result.Id);
            Assert.Equal(EstadoSesionEntrenamiento.EnCurso, result.Estado);
            Assert.Single(result.Ejercicios);

            var mappedExercise = Assert.Single(result.Ejercicios);
            Assert.Equal("Press banca con barra", mappedExercise.Nombre);
            Assert.Single(mappedExercise.Series);

            var mappedSet = Assert.Single(mappedExercise.Series);
            Assert.Equal(1, mappedSet.NumeroSerie);
            Assert.Equal(80m, mappedSet.Peso);
            Assert.Equal(10, mappedSet.Repeticiones);
            Assert.Equal(2, mappedSet.Rir);
            Assert.True(mappedSet.Completada);
            Assert.Null(mappedSet.RepeticionesObjetivoMinimas);
            Assert.Null(mappedSet.RepeticionesObjetivoMaximas);
            Assert.Null(mappedSet.RirObjetivoMinimo);
            Assert.Null(mappedSet.RirObjetivoMaximo);
        }
    }
}
