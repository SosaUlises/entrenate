using System.Reflection;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using Entrenate.Domain.Entidades;
using MediatR;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Adaptation
{
    public sealed class LivePrescriptionAdaptationTests
    {
        private static readonly DateTime NowUtc = new(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Evaluate_RepeatedUnderperformance_ReducesFutureRangeByTwoWithSafeFloor()
        {
            var session = CreateSession();
            RegisterMissedSets(session);

            var result = LivePrescriptionAdaptationEngine.Evaluate(
                session, "user", LivePerformanceSignalType.MissedRepetitions, NowUtc);

            var proposal = Assert.IsType<LivePrescriptionAdaptationProposal>(result.Proposal);
            Assert.Equal(LivePrescriptionAdaptationStatus.Proposal, result.Status);
            Assert.Equal(3, proposal.EffectiveFromSetNumber);
            Assert.Equal((8, 10), (proposal.PreviousRepMin, proposal.PreviousRepMax));
            Assert.Equal((6, 8), (proposal.NewRepMin, proposal.NewRepMax));
            Assert.Equal((1, 2), (proposal.NewRirMin, proposal.NewRirMax));
        }

        [Fact]
        public void Evaluate_RepeatedExcessiveEffort_IncreasesFutureRirMarginByOne()
        {
            var session = CreateSession();
            var exercise = session.Ejercicios.Single();
            exercise.RegistrarOActualizarSerie(1, 100, 8, 0, NowUtc.AddMinutes(-2));
            exercise.RegistrarOActualizarSerie(2, 100, 8, 0, NowUtc.AddMinutes(-1));

            var result = LivePrescriptionAdaptationEngine.Evaluate(
                session, "user", LivePerformanceSignalType.ExcessiveEffort, NowUtc);

            var proposal = Assert.IsType<LivePrescriptionAdaptationProposal>(result.Proposal);
            Assert.Equal((8, 10), (proposal.NewRepMin, proposal.NewRepMax));
            Assert.Equal((2, 3), (proposal.NewRirMin, proposal.NewRirMax));
        }

        [Fact]
        public void AdaptarPrescripcionPendiente_PreservesCompletedSnapshotsAndPerformance()
        {
            var session = CreateSession();
            RegisterMissedSets(session);
            var exercise = session.Ejercicios.Single();

            session.AdaptarPrescripcionPendiente(
                exercise.Id, 3, 8, 10, 1, 2, 6, 8, 1, 2);

            Assert.Equal((6, 8),
                (exercise.RepeticionesMinimasObjetivo, exercise.RepeticionesMaximasObjetivo));
            Assert.All(exercise.Series, set =>
            {
                Assert.Equal((8, 10),
                    (set.RepeticionesObjetivoMinimas, set.RepeticionesObjetivoMaximas));
                Assert.Equal((1, 2), (set.RirObjetivoMinimo, set.RirObjetivoMaximo));
                Assert.Equal(6, set.Repeticiones);
                Assert.Equal(100, set.Peso);
            });
            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            Assert.Equal(exercise.Id, current?.ExerciseSession.Id);
            Assert.Equal(3, current?.SetNumber);
        }

        [Fact]
        public async Task Apply_WhenProposalIsCurrent_UpdatesPendingPrescriptionAndSaves()
        {
            var session = CreateSession();
            RegisterMissedSets(session);
            var proposal = LivePrescriptionAdaptationEngine.Evaluate(
                session, "user", LivePerformanceSignalType.MissedRepetitions, DateTime.UtcNow).Proposal!;
            var repository = new FakeRepository(session);
            var handler = new ApplyLivePrescriptionAdaptationCommandHandler(
                new StubCurrentUser("user"), repository, new StubSender(ToDto(session)));

            var result = await handler.Handle(
                new ApplyLivePrescriptionAdaptationCommand(proposal), CancellationToken.None);

            Assert.Equal(session.Id, result.UpdatedSession.Id);
            Assert.Contains("desde la serie 3 quedó en 6 a 8 repeticiones y RIR 1 a 2", result.Text);
            Assert.Contains("Quedan 2 series. Sigue Hip Thrust, serie 3", result.Text);
            Assert.Equal(1, repository.SaveCount);
            Assert.Equal(6, session.Ejercicios.Single().RepeticionesMinimasObjetivo);
            var audit = Assert.Single(session.AdaptacionesLiveCoach);
            Assert.Equal(3, audit.SerieEfectivaDesde);
            Assert.Equal(6, audit.RepeticionesMinimasNuevas);
        }

        [Fact]
        public async Task Apply_WhenAnotherSetWasRegistered_RejectsStaleProposal()
        {
            var session = CreateSession();
            RegisterMissedSets(session);
            var proposal = LivePrescriptionAdaptationEngine.Evaluate(
                session, "user", LivePerformanceSignalType.MissedRepetitions, DateTime.UtcNow).Proposal!;
            session.Ejercicios.Single().RegistrarOActualizarSerie(3, 100, 6, 0, NowUtc);
            var repository = new FakeRepository(session);
            var handler = new ApplyLivePrescriptionAdaptationCommandHandler(
                new StubCurrentUser("user"), repository, new StubSender(ToDto(session)));

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new ApplyLivePrescriptionAdaptationCommand(proposal), CancellationToken.None));
            Assert.Equal(0, repository.SaveCount);
            Assert.Empty(session.AdaptacionesLiveCoach);
        }

        private static void RegisterMissedSets(SesionEntrenamiento session)
        {
            var exercise = session.Ejercicios.Single();
            exercise.RegistrarOActualizarSerie(1, 100, 6, 0, NowUtc.AddMinutes(-2));
            exercise.RegistrarOActualizarSerie(2, 100, 6, 0, NowUtc.AddMinutes(-1));
        }

        private static SesionEntrenamiento CreateSession()
        {
            var exercise = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Hip Thrust",
                GrupoMuscularPrincipal = "Gluteos",
                Activo = true
            };
            var sessionExercise = new EjercicioSesion(
                exercise.Id, 1, 4, 8, 10, 1, 2, 120);
            typeof(EjercicioSesion).GetProperty(
                "Ejercicio", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(sessionExercise, exercise);
            return new SesionEntrenamiento(
                "user", Guid.NewGuid(), NowUtc.AddMinutes(-20), [sessionExercise]);
        }

        private static TrainingSessionDto ToDto(SesionEntrenamiento session)
            => new(session.Id, session.DiaRutinaId, session.Fecha, session.HoraInicio,
                session.HoraFin, session.Estado, []);

        private sealed class StubCurrentUser(string userId) : ICurrentUserService
        {
            public string? UserId { get; } = userId;
        }

        private sealed class StubSender(TrainingSessionDto response) : ISender
        {
            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Assert.IsType<GetTrainingSessionByIdQuery>(request);
                return Task.FromResult((TResponse)(object)response);
            }
            public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotSupportedException();
            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class FakeRepository(SesionEntrenamiento session) : ITrainingSessionRepository
        {
            public int SaveCount { get; private set; }
            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
                => Task.FromResult<SesionEntrenamiento?>(session.Id == sessionId && session.UsuarioId == userId ? session : null);
            public Task SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.CompletedTask; }
            public Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<SesionEntrenamiento?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>> GetHistoryByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IReadOnlyCollection<DateTime>> GetCompletedSessionStartTimesByUserIdAsync(string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsNoTrackingAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressTrendByUserIdAsync(Guid exerciseId, string userId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<bool> HasCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IReadOnlyCollection<ExerciseProgressHubSourceDto>> GetCompletedExerciseProgressHubByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IReadOnlyCollection<ProgressExposure>> GetRecentCompletedExerciseExposuresByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task AddAsync(SesionEntrenamiento value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<AdaptiveSessionPersistenceResult> ConfirmAdaptiveSessionAsync(SesionEntrenamiento value, SesionAdaptacion adaptation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
