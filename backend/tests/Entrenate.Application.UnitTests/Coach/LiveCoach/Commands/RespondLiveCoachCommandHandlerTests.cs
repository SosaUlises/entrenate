using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.LiveCoach.Commands.RespondLiveCoach;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Coach.LiveCoach.Prompt;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Application.Coach.LiveCoach.Tools;
using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Common.Models;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Commands
{
    public sealed class RespondLiveCoachCommandHandlerTests
    {
        private const string TestUserId = "user-live-coach-1";

        private readonly FakeTrainingSessionRepository _sessionRepository;
        private readonly FakeCurrentUserService _currentUserService;
        private readonly FakeLiveCoachResponseCache _responseCache;
        private readonly FakeCoachConversationProvider _conversationProvider;
        private readonly LiveCoachToolRegistry _toolRegistry;
        private readonly LiveCoachContextBuilder _contextBuilder;
        private readonly LiveCoachIntentRouter _intentRouter;
        private readonly LiveCoachPromptBuilder _promptBuilder;

        private readonly RespondLiveCoachCommandHandler _handler;

        public RespondLiveCoachCommandHandlerTests()
        {
            _sessionRepository = new FakeTrainingSessionRepository();
            _currentUserService = new FakeCurrentUserService(TestUserId);
            _responseCache = new FakeLiveCoachResponseCache();
            _conversationProvider = new FakeCoachConversationProvider();
            _toolRegistry = new LiveCoachToolRegistry(_sessionRepository);
            _contextBuilder = new LiveCoachContextBuilder();
            _intentRouter = new LiveCoachIntentRouter();
            _promptBuilder = new LiveCoachPromptBuilder();

            _handler = new RespondLiveCoachCommandHandler(
                _sessionRepository,
                _currentUserService,
                _contextBuilder,
                _toolRegistry,
                _intentRouter,
                _promptBuilder,
                _conversationProvider,
                _responseCache,
                NullLogger<RespondLiveCoachCommandHandler>.Instance);
        }

        [Fact]
        public async Task Handle_WhenUserNotAuthenticated_ThrowsUnauthorizedAccessException()
        {
            _currentUserService.UserId = string.Empty;

            var command = new RespondLiveCoachCommand(
                Guid.NewGuid(),
                "turn-1",
                "audio",
                "¿Qué me toca ahora?",
                null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenSessionNotFound_ThrowsNotFoundException()
        {
            var command = new RespondLiveCoachCommand(
                Guid.NewGuid(),
                "turn-1",
                "audio",
                "¿Qué me toca ahora?",
                null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenSessionNotActive_ThrowsConflictException()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.Completada);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-1",
                "audio",
                "¿Qué ejercicio sigue?",
                null);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                _handler.Handle(command, CancellationToken.None));

            Assert.Contains("activa", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_WhenQueryIsOutOfScope_ReturnsDirectAnswerWithoutInvokingLlm()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-out-of-scope",
                "audio",
                "¿Qué opinás del clima hoy en Buenos Aires?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("turn-out-of-scope", result.ClientTurnId);
            Assert.Contains("sesión activa de entrenamiento", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _conversationProvider.CallCount);
            Assert.True(_responseCache.Cache.ContainsKey(result.ResponseId));
        }

        [Fact]
        public async Task Handle_WhenQueryIsMutationRequest_ReturnsDirectSafeAnswerWithoutInvokingLlm()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-mut",
                "audio",
                "Anotame 4 repeticiones con 80 kilos",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("turn-mut", result.ClientTurnId);
            Assert.Contains("registro de series", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _conversationProvider.CallCount);
            Assert.True(_responseCache.Cache.ContainsKey(result.ResponseId));
        }

        [Fact]
        public async Task Handle_WhenValidSessionAndQuery_ReturnsGroundedAnswerAndCachesResponse()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            _conversationProvider.ResponseToReturn = "Te tocan 4 series de 8 a 10 repeticiones con RIR 2.";

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-ok",
                "audio",
                "¿Cuántas series me tocan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("turn-ok", result.ClientTurnId);
            Assert.Equal("Te tocan 4 series de 8 a 10 repeticiones con RIR 2.", result.Text);
            Assert.Equal(1, _conversationProvider.CallCount);
            Assert.False(result.IsStaleContext);
            Assert.True(result.CanSynthesizeVoice);
            Assert.True(_responseCache.Cache.ContainsKey(result.ResponseId));

            // Verify prompt groundings
            Assert.NotNull(_conversationProvider.LastPrompt);
            Assert.Contains("Press de banca", _conversationProvider.LastPrompt.Input);
            Assert.Contains("¿Cuántas series me tocan?", _conversationProvider.LastPrompt.Input);
        }

        [Fact]
        public async Task Handle_WhenClientSnapshotDriftDetected_FlagsIsStaleContext()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            _conversationProvider.ResponseToReturn = "Estás en el Press de banca en tu primera serie.";

            var clientSnapshot = new ClientSnapshotInput(
                ExerciseId: Guid.NewGuid(), // Different from session current exercise
                SetIndex: 5,
                RestIsActive: false,
                RestRemainingSeconds: 30,
                RestCapturedAtUtc: DateTime.UtcNow.AddSeconds(-15));

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-drift",
                "audio",
                "¿En qué ejercicio estoy?",
                clientSnapshot);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsStaleContext);
            Assert.Equal(1, _conversationProvider.CallCount);
            Assert.Contains("avanzó de ejercicio", _conversationProvider.LastPrompt!.Input, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Handle_WhenQueryRequiresPreviousPerformance_QueriesToolAndIncludesInPrompt()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var exerciseId = session.Ejercicios.GetEnumerator();
            exerciseId.MoveNext();
            var targetExerciseId = exerciseId.Current.EjercicioId;

            _sessionRepository.Exposures[targetExerciseId] =
            [
                new ProgressExposure(
                    Guid.NewGuid(),
                    session.Id,
                    targetExerciseId,
                    DateTime.UtcNow.AddDays(-7),
                    new ProgressPrescriptionSnapshot(4, 8, 10, 1, 2, 120),
                    [
                        new ProgressCompletedSet(1, 80m, 10, 2),
                        new ProgressCompletedSet(2, 80m, 9, 2)
                    ])
            ];

            _conversationProvider.ResponseToReturn = "La última vez hiciste 80 kilos por 10 repeticiones.";

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-prev",
                "audio",
                "¿Cuánto levanté la última vez en este ejercicio?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("turn-prev", result.ClientTurnId);
            Assert.Equal(1, _conversationProvider.CallCount);
            Assert.Contains("80", _conversationProvider.LastPrompt!.Input);
        }

        [Fact]
        public async Task Handle_WhenProviderFails_ThrowsExternalServiceException()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            _conversationProvider.ThrowOnCall = true;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-fail",
                "audio",
                "¿Qué descanso me toca?",
                null);

            await Assert.ThrowsAsync<ExternalServiceException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        private static SesionEntrenamiento CreateSession(string userId, EstadoSesionEntrenamiento estado)
        {
            var exercise1 = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Press de banca",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };

            var exerciseSesion1 = new EjercicioSesion(
                exercise1.Id,
                orden: 1,
                seriesObjetivo: 4,
                repeticionesMinimasObjetivo: 8,
                repeticionesMaximasObjetivo: 10,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoObjetivoSegundos: 120,
                notas: "Cuidar técnica");

            typeof(EjercicioSesion)
                .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)?
                .SetValue(exerciseSesion1, exercise1);

            var session = new SesionEntrenamiento(
                userId,
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                [exerciseSesion1]);

            if (estado == EstadoSesionEntrenamiento.Completada)
            {
                // Set Estado via reflection since setter is private
                typeof(SesionEntrenamiento)
                    .GetProperty("Estado", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(session, EstadoSesionEntrenamiento.Completada);
            }

            return session;
        }

        private sealed class FakeCurrentUserService : ICurrentUserService
        {
            public string? UserId { get; set; }
            public FakeCurrentUserService(string? userId) => UserId = userId;
        }

        private sealed class FakeLiveCoachResponseCache : ILiveCoachResponseCache
        {
            public Dictionary<Guid, string> Cache { get; } = [];

            public void Set(Guid responseId, string text, TimeSpan ttl)
            {
                Cache[responseId] = text;
            }

            public bool TryGet(Guid responseId, out string? text)
            {
                return Cache.TryGetValue(responseId, out text);
            }
        }

        private sealed class FakeCoachConversationProvider : ICoachConversationProvider
        {
            public int CallCount { get; private set; }
            public CoachConversationPrompt? LastPrompt { get; private set; }
            public string ResponseToReturn { get; set; } = "Respuesta del coach.";
            public bool ThrowOnCall { get; set; }

            public Task<CoachConversationResponse> GenerateResponseAsync(
                CoachConversationPrompt prompt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ThrowOnCall)
                {
                    throw new InvalidOperationException("Groq API timeout simulation.");
                }

                CallCount++;
                LastPrompt = prompt;

                return Task.FromResult(new CoachConversationResponse(ResponseToReturn));
            }
        }

        private sealed class FakeTrainingSessionRepository : ITrainingSessionRepository
        {
            public Dictionary<Guid, SesionEntrenamiento> Sessions { get; } = [];
            public Dictionary<Guid, IReadOnlyCollection<ProgressExposure>> Exposures { get; } = [];

            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsNoTrackingAsync(
                Guid sessionId,
                string userId,
                CancellationToken cancellationToken = default)
            {
                if (Sessions.TryGetValue(sessionId, out var session) && session.UsuarioId == userId)
                {
                    return Task.FromResult<SesionEntrenamiento?>(session);
                }

                return Task.FromResult<SesionEntrenamiento?>(null);
            }

            public Task<IReadOnlyCollection<ProgressExposure>> GetRecentCompletedExerciseExposuresByUserIdAsync(
                Guid exerciseId,
                string userId,
                CancellationToken cancellationToken = default)
            {
                if (Exposures.TryGetValue(exerciseId, out var exposures))
                {
                    return Task.FromResult(exposures);
                }

                return Task.FromResult<IReadOnlyCollection<ProgressExposure>>([]);
            }

            public Task AddAsync(SesionEntrenamiento session, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<AdaptiveSessionPersistenceResult> ConfirmAdaptiveSessionAsync(SesionEntrenamiento session, SesionAdaptacion adaptation, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<SesionEntrenamiento?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<DateTime>> GetCompletedSessionStartTimesByUserIdAsync(string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>> GetHistoryByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressTrendByUserIdAsync(Guid exerciseId, string userId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<IReadOnlyCollection<ExerciseProgressHubSourceDto>> GetCompletedExerciseProgressHubByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<bool> HasCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
