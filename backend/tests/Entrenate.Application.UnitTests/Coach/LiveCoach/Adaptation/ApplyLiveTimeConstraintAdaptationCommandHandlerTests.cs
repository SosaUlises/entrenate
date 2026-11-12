using System.Reflection;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using MediatR;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Adaptation
{
    public sealed class ApplyLiveTimeConstraintAdaptationCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenProposalIsCurrent_OmitsAllChangesAndSavesOnce()
        {
            var session = CreateSession();
            var proposal = CreateProposal(session);
            var repository = new FakeRepository(session);
            var handler = new ApplyLiveTimeConstraintAdaptationCommandHandler(
                new StubCurrentUser("user"),
                repository,
                new StubSender(ToDto(session)));

            var result = await handler.Handle(
                new ApplyLiveTimeConstraintAdaptationCommand(proposal),
                CancellationToken.None);

            Assert.True(session.Ejercicios.Last().Omitido);
            Assert.False(session.Ejercicios.First().Omitido);
            Assert.Equal(1, repository.SaveCount);
            Assert.Equal(session.Id, result.Id);
        }

        [Fact]
        public async Task Handle_WhenAnotherSetWasCompleted_RejectsStaleProposalWithoutSaving()
        {
            var session = CreateSession();
            var proposal = CreateProposal(session);
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 8, 1, DateTime.UtcNow);
            var repository = new FakeRepository(session);
            var handler = CreateHandler(repository, session);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new ApplyLiveTimeConstraintAdaptationCommand(proposal),
                CancellationToken.None));

            Assert.Equal(0, repository.SaveCount);
            Assert.All(session.Ejercicios, exercise => Assert.False(exercise.Omitido));
        }

        [Fact]
        public async Task Handle_WhenOmissionChanged_RejectsStaleProposal()
        {
            var session = CreateSession();
            var proposal = CreateProposal(session);
            session.OmitirEjercicio(session.Ejercicios.ElementAt(1).Id);
            var repository = new FakeRepository(session);
            var handler = CreateHandler(repository, session);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new ApplyLiveTimeConstraintAdaptationCommand(proposal),
                CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenProposalBelongsToAnotherUser_DoesNotExposeIt()
        {
            var session = CreateSession();
            var repository = new FakeRepository(session);
            var handler = new ApplyLiveTimeConstraintAdaptationCommandHandler(
                new StubCurrentUser("other"),
                repository,
                new StubSender(ToDto(session)));

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new ApplyLiveTimeConstraintAdaptationCommand(CreateProposal(session)),
                CancellationToken.None));
        }

        private static ApplyLiveTimeConstraintAdaptationCommandHandler CreateHandler(
            FakeRepository repository,
            SesionEntrenamiento session)
            => new(new StubCurrentUser("user"), repository, new StubSender(ToDto(session)));

        private static LiveTimeConstraintProposal CreateProposal(SesionEntrenamiento session)
        {
            var change = new LiveTimeConstraintChange(
                LiveTimeConstraintChangeType.OmitExercise,
                session.Ejercicios.Last().Id,
                session.Ejercicios.Last().Ejercicio.Nombre);
            return new(
                Guid.NewGuid(),
                "user",
                session.Id,
                DateTime.UtcNow,
                DateTime.UtcNow.AddSeconds(45),
                8,
                12,
                6,
                LiveTimeConstraintFingerprint.Create(session),
                [change]);
        }

        private static SesionEntrenamiento CreateSession()
        {
            var exercises = new[] { "Press", "Remo", "Curl" }
                .Select((name, index) =>
                {
                    var exercise = new Ejercicio
                    {
                        Id = Guid.NewGuid(),
                        Nombre = name,
                        GrupoMuscularPrincipal = "General",
                        Activo = true
                    };
                    var sessionExercise = new EjercicioSesion(
                        exercise.Id, index + 1, 3, 8, 10, 1, 2, 90);
                    typeof(EjercicioSesion).GetProperty(
                        "Ejercicio",
                        BindingFlags.Public | BindingFlags.Instance)!.SetValue(sessionExercise, exercise);
                    return sessionExercise;
                });
            return new SesionEntrenamiento(
                "user",
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                exercises);
        }

        private static TrainingSessionDto ToDto(SesionEntrenamiento session)
            => new(
                session.Id,
                session.DiaRutinaId,
                session.Fecha,
                session.HoraInicio,
                session.HoraFin,
                session.Estado,
                []);

        private sealed class StubCurrentUser(string userId) : ICurrentUserService
        {
            public string? UserId { get; } = userId;
        }

        private sealed class StubSender(TrainingSessionDto response) : ISender
        {
            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Assert.IsType<GetTrainingSessionByIdQuery>(request);
                return Task.FromResult((TResponse)(object)response);
            }

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
                where TRequest : IRequest
                => throw new NotSupportedException();

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private sealed class FakeRepository(SesionEntrenamiento session) : ITrainingSessionRepository
        {
            public int SaveCount { get; private set; }

            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(
                Guid sessionId,
                string userId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<SesionEntrenamiento?>(
                    session.Id == sessionId && session.UsuarioId == userId ? session : null);

            public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveCount++;
                return Task.CompletedTask;
            }

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
            public Task AddAsync(SesionEntrenamiento session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<AdaptiveSessionPersistenceResult> ConfirmAdaptiveSessionAsync(SesionEntrenamiento session, SesionAdaptacion adaptation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
