using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.LiveCoach.Commands.RespondLiveCoach;
using Entrenate.Application.Coach.LiveCoach.Actions;
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
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
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
        private readonly LiveCoachSetActionExtractor _setActionExtractor;
        private readonly FakePendingActionStore _pendingActionStore;
        private readonly FakePendingActionExecutor _pendingActionExecutor;

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
            _setActionExtractor = new LiveCoachSetActionExtractor();
            _pendingActionStore = new FakePendingActionStore();
            _pendingActionExecutor = new FakePendingActionExecutor(_pendingActionStore);

            _handler = new RespondLiveCoachCommandHandler(
                _sessionRepository,
                _currentUserService,
                _contextBuilder,
                _toolRegistry,
                _intentRouter,
                _promptBuilder,
                _conversationProvider,
                _responseCache,
                _setActionExtractor,
                _pendingActionStore,
                _pendingActionExecutor,
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
        public async Task Handle_WhenRegisterCurrentSetIsValid_CreatesPendingActionWithoutExecutingMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-mut",
                "audio",
                "Anotame 4 repeticiones con 80 kilos, RIR 2",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("turn-mut", result.ClientTurnId);
            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.RegisterCurrentSet, result.PendingAction.Type);
            Assert.Contains("80 kg", result.PendingAction.Summary);
            Assert.Contains("RIR 2", result.PendingAction.Summary);
            Assert.True(_pendingActionStore.Actions.ContainsKey(result.PendingAction.ActionId));
            Assert.Empty(session.Ejercicios.Single().Series);
            Assert.Equal(0, _conversationProvider.CallCount);
            Assert.True(_responseCache.Cache.ContainsKey(result.ResponseId));
        }

        [Fact]
        public async Task Handle_WhenRegisterCurrentSetIsMissingWeight_AsksForWeightWithoutPendingAction()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-missing", "audio", "Hice 8 repeticiones", null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.Contains("peso", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(_pendingActionStore.Actions);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNoCurrentSetExists_DoesNotCreatePendingAction()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.Single();
            for (var setNumber = 1; setNumber <= exercise.SeriesObjetivo; setNumber++)
                exercise.RegistrarOActualizarSerie(setNumber, 80m, 8, 2, DateTime.UtcNow);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-complete", "audio", "Hice 8 con 80 kilos", null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.Contains("serie pendiente", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(_pendingActionStore.Actions);
        }

        [Theory]
        [InlineData("sí")]
        [InlineData("dale")]
        public async Task Handle_WhenPendingActionExists_ExplicitVoiceConfirmationConfirmsIt(
            string confirmation)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-confirm", "audio", confirmation, null),
                CancellationToken.None);

            Assert.Equal("Serie registrada.", result.Text);
            Assert.Null(result.PendingAction);
            Assert.NotNull(result.UpdatedSession);
            Assert.Equal(session.Id, result.UpdatedSession.Id);
            Assert.Equal(1, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(0, _conversationProvider.CallCount);
            Assert.Empty(_pendingActionStore.Actions);
        }

        [Fact]
        public async Task Handle_WhenPendingActionExists_ExplicitVoiceCancellationCancelsWithoutTrainingMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-cancel", "audio", "no", null),
                CancellationToken.None);

            Assert.Equal("Listo, no la registro.", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Null(result.UpdatedSession);
            Assert.Equal(1, _pendingActionExecutor.CancelCount);
            Assert.Equal(0, _pendingActionExecutor.ConfirmCount);
            Assert.Empty(session.Ejercicios.Single().Series);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("sí")]
        [InlineData("no")]
        public async Task Handle_WhenNoPendingActionExists_YesOrNoDoesNotMutateOrInvokeProvider(string answer)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-no-pending", "audio", answer, null),
                CancellationToken.None);

            Assert.Contains("no hay una serie pendiente", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(0, _pendingActionExecutor.CancelCount);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPendingActionExists_AmbiguousInputKeepsPendingAndAsksForClarification()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var pending = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-ambiguous", "audio", "puede ser", null),
                CancellationToken.None);

            Assert.Contains("Decime sí o no", result.Text);
            Assert.Equal(pending.ActionId, result.PendingAction?.ActionId);
            Assert.Equal(0, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(0, _pendingActionExecutor.CancelCount);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPendingActionExpired_YesReturnsStaleWithoutMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var action = new PendingLiveCoachAction(
                Guid.NewGuid(), TestUserId, session.Id, session.Ejercicios.Single().Id,
                1, 100m, 8, 1, DateTime.UtcNow.AddSeconds(-1));
            _pendingActionStore.Set(action);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-expired", "audio", "sí", null),
                CancellationToken.None);

            Assert.Contains("ya no está vigente", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPendingActionBecameStale_YesReturnsStaleWithoutRetrying()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);
            _pendingActionExecutor.ThrowConflict = true;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-stale", "audio", "confirmo", null),
                CancellationToken.None);

            Assert.Contains("ya no está vigente", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(1, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenConfirmationWasAlreadyConsumed_SecondConfirmationCannotDuplicateMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);

            await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-first", "audio", "sí", null),
                CancellationToken.None);
            var second = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-second", "audio", "dale", null),
                CancellationToken.None);

            Assert.Equal(1, _pendingActionExecutor.ConfirmCount);
            Assert.Contains("no hay una serie pendiente", second.Text, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Hice 8 con 100 kilos, RIR uno.", 8, 100, 1)]
        [InlineData("Registrá 10 reps con 80 kg y RIR 2.", 10, 80, 2)]
        [InlineData("Terminé, fueron 12 con 20 kilos.", 12, 20, null)]
        public void SetActionExtractor_ParsesSupportedNaturalPhrases(
            string text,
            int repetitions,
            decimal weight,
            int? rir)
        {
            var result = _setActionExtractor.Extract(text);

            Assert.True(result.IsValid);
            Assert.Equal(repetitions, result.Repetitions);
            Assert.Equal(weight, result.WeightKg);
            Assert.Equal(rir, result.Rir);
        }

        [Fact]
        public void IntentRouter_WhenMutationIsNotRegisterCurrentSet_KeepsItBlocked()
        {
            var result = _intentRouter.Route("Cambiame el próximo ejercicio");

            Assert.Equal(LiveCoachIntentKind.OtherMutation, result.Kind);
            Assert.Contains("solo puedo registrar", result.DirectAnswer!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpsertTrainingSet_WhenLiveCoachTargetIsStale_DoesNotOverwriteSet()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.Single();
            exercise.RegistrarOActualizarSerie(1, 90m, 8, 2, DateTime.UtcNow);
            var handler = new UpsertTrainingSetCommandHandler(_currentUserService, _sessionRepository);

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new UpsertTrainingSetCommand(
                    session.Id, exercise.Id, 1, 100m, 8, 1,
                    RequireCurrentPendingSet: true),
                CancellationToken.None));

            Assert.Equal(90m, exercise.Series.Single().Peso);
        }

        [Fact]
        public async Task UpsertTrainingSet_WhenLiveCoachTargetIsCurrent_UsesRealTrainingMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.Single();
            var handler = new UpsertTrainingSetCommandHandler(_currentUserService, _sessionRepository);

            var result = await handler.Handle(
                new UpsertTrainingSetCommand(
                    session.Id, exercise.Id, 1, 100m, 8, 1,
                    RequireCurrentPendingSet: true),
                CancellationToken.None);

            Assert.Equal(100m, result.Peso);
            Assert.Equal(8, result.Repeticiones);
            Assert.Single(exercise.Series);
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
        public async Task Handle_WhenStartRestIsExplicit_ReturnsPrescribedClientActionWithoutProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            CompleteSets(session.Ejercicios.First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-start", "audio", "Arrancá el descanso", null), CancellationToken.None);

            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(150, result.ClientAction?.DurationSeconds);
            Assert.Equal("Descanso iniciado: 150 segundos.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("inicie el descanso")]
        [InlineData("Iniciá el descanso")]
        [InlineData("inicia el descanso")]
        [InlineData("iniciar el descanso")]
        [InlineData("iniciar descanso")]
        [InlineData("Arrancá el descanso")]
        [InlineData("arranca el descanso")]
        [InlineData("Empezá el descanso")]
        [InlineData("empeza el descanso")]
        [InlineData("Poné el descanso")]
        [InlineData("Arrancá el timer")]
        [InlineData("Iniciá el timer")]
        [InlineData("¡Inicie el descanso!")]
        [InlineData("  INICIE   el  descanso. ")]
        [InlineData("iniciame el descanso")]
        [InlineData("por favor iniciá el descanso")]
        [InlineData("iniciá el descanso porfa")]
        [InlineData("iniciá el descanso ya")]
        [InlineData("iniciá el descanso ahora")]
        [InlineData("che poné el descanso")]
        [InlineData("dale arrancá el timer")]
        [InlineData("activá el descanso")]
        [InlineData("activar descanso")]
        [InlineData("iniciá el temporizador")]
        [InlineData("poné el temporizador")]
        [InlineData("largá el descanso")]
        [InlineData("iniciá el cronómetro")]
        [InlineData("iniciá un descanso")]
        [InlineData("iniciá el tiempo de descanso")]
        [InlineData("poneme el descanso")]
        [InlineData("poneme el timer")]
        public void IntentRouter_WhenStartRestPhrase_RoutesDeterministicallyToStartRestTimer(string phrase)
        {
            var routing = _intentRouter.Route(phrase);

            Assert.Equal(LiveCoachIntentKind.StartRestTimer, routing.Kind);
        }

        [Theory]
        [InlineData("pará el descanso")]
        [InlineData("para el descanso")]
        [InlineData("parame el descanso")]
        [InlineData("parar descanso")]
        [InlineData("parar el descanso")]
        [InlineData("detené el descanso")]
        [InlineData("deteneme el descanso")]
        [InlineData("detener descanso")]
        [InlineData("detener el descanso")]
        [InlineData("detené el timer")]
        [InlineData("terminar descanso")]
        [InlineData("terminar el descanso")]
        [InlineData("terminá el descanso")]
        [InlineData("termina el descanso")]
        [InlineData("termine el descanso")]
        [InlineData("finalizar descanso")]
        [InlineData("finalizá el descanso")]
        [InlineData("finalizar el descanso")]
        [InlineData("frená el descanso")]
        [InlineData("frenar timer")]
        [InlineData("cancelar descanso")]
        [InlineData("cancelar el descanso")]
        [InlineData("cancelá el descanso")]
        [InlineData("apagar timer")]
        [InlineData("sacá el timer")]
        [InlineData("sacar timer")]
        [InlineData("apagá el timer")]
        [InlineData("cortá el cronómetro")]
        [InlineData("por favor frená el descanso")]
        [InlineData("che, terminá el descanso por favor")]
        public void IntentRouter_WhenStopRestPhrase_RoutesDeterministicallyToStopRestTimer(string phrase)
        {
            var routing = _intentRouter.Route(phrase);

            Assert.Equal(LiveCoachIntentKind.StopRestTimer, routing.Kind);
        }

        [Theory]
        [InlineData("¿Cuánto descanso me queda?")]
        [InlineData("cuánto descanso tengo")]
        [InlineData("necesito un descanso largo")]
        [InlineData("poné el descanso en 2 minutos")]
        [InlineData("inicie el descanso de 90 segundos")]
        public void IntentRouter_WhenPhraseIsNotExactStartRest_DoesNotRouteToStartRestTimer(string phrase)
        {
            var routing = _intentRouter.Route(phrase);

            Assert.NotEqual(LiveCoachIntentKind.StartRestTimer, routing.Kind);
        }

        [Theory]
        [InlineData("¿cuánto descanso me queda?")]
        [InlineData("¿está activo el descanso?")]
        [InlineData("¿terminó el descanso?")]
        [InlineData("¿ya terminó el descanso?")]
        [InlineData("¿ya terminó?")]
        [InlineData("¿cuánto falta?")]
        [InlineData("¿qué descanso me toca?")]
        public void IntentRouter_WhenRestIsQueried_RoutesToReadOnlyRestTimerQuery(string phrase)
        {
            var routing = _intentRouter.Route(phrase);

            Assert.Equal(LiveCoachIntentKind.RestTimerQuery, routing.Kind);
        }

        [Theory]
        [InlineData("poné el descanso en 2 minutos")]
        [InlineData("arrancá 90 segundos")]
        [InlineData("sumale 30 segundos")]
        [InlineData("restale 20")]
        public void IntentRouter_WhenCustomRestDurationIsRequested_RoutesToUnsupportedRestMutation(string phrase)
        {
            var routing = _intentRouter.Route(phrase);

            Assert.Equal(LiveCoachIntentKind.UnsupportedRestDurationMutation, routing.Kind);
            Assert.Equal("Por ahora uso el tiempo de descanso indicado por la rutina.", routing.DirectAnswer);
        }

        [Fact]
        public async Task Handle_WhenInicieElDescanso_StartsRestWithoutInvokingProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            CompleteSets(session.Ejercicios.First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-inicie", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(150, result.ClientAction?.DurationSeconds);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseA_WhenFirstSetHasNoData_DoesNotStartRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-case-a", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseB_WhenSetPartiallyLoaded_DoesNotStartRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            AddIncompleteSet(session.Ejercicios.First(), setNumber: 1, completada: false, reps: 0, peso: 80);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-case-b", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseC_WhenSetExistsButNotCompleted_DoesNotStartRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            AddIncompleteSet(session.Ejercicios.First(), setNumber: 1, completada: false, reps: 10, peso: 80);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-case-c", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseD_WhenSetCompletedAccordingToTraining_StartsRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            CompleteSets(session.Ejercicios.First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-case-d", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(150, result.ClientAction?.DurationSeconds);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseE_WhenSetOneCompletedAndSetTwoPending_StartsRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            CompleteSets(session.Ejercicios.First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-case-e", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(150, result.ClientAction?.DurationSeconds);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseF_WhenSetOneIncompleteAndSetTwoStructurePresent_DoesNotStartRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            AddIncompleteSet(session.Ejercicios.First(), setNumber: 1, completada: false, reps: 0, peso: 0);
            AddIncompleteSet(session.Ejercicios.First(), setNumber: 2, completada: false, reps: 0, peso: 0);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-case-f", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_CaseG_WhenExerciseBoundary_LastSetOfPreviousExerciseMustBeReallyCompleted()
        {
            // G1: Serie del ejercicio anterior incompleta -> no inicia
            var sessionIncomplete = CreateTwoExerciseSession(firstRest: 90, secondRest: 180);
            AddIncompleteSet(sessionIncomplete.Ejercicios.OrderBy(e => e.Orden).First(), setNumber: 1, completada: false, reps: 0);
            _sessionRepository.Sessions[sessionIncomplete.Id] = sessionIncomplete;

            var resultIncomplete = await _handler.Handle(new RespondLiveCoachCommand(
                sessionIncomplete.Id, "turn-case-g1", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(resultIncomplete.ClientAction);
            Assert.Equal("Primero completá la serie actual.", resultIncomplete.Text);
            Assert.Equal(0, _conversationProvider.CallCount);

            // G2: Serie del ejercicio anterior realmente completada -> inicia descanso con duración del ejercicio anterior
            var sessionComplete = CreateTwoExerciseSession(firstRest: 90, secondRest: 180);
            CompleteSets(sessionComplete.Ejercicios.OrderBy(e => e.Orden).First(), 1);
            _sessionRepository.Sessions[sessionComplete.Id] = sessionComplete;

            var resultComplete = await _handler.Handle(new RespondLiveCoachCommand(
                sessionComplete.Id, "turn-case-g2", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Equal("StartRestTimer", resultComplete.ClientAction?.Type);
            Assert.Equal(90, resultComplete.ClientAction?.DurationSeconds);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("inicie el descanso")]
        [InlineData("iniciame el descanso")]
        [InlineData("por favor iniciá el descanso")]
        [InlineData("poné el timer")]
        [InlineData("poneme el descanso")]
        [InlineData("activá el descanso")]
        [InlineData("iniciá el temporizador")]
        [InlineData("largá el descanso")]
        public async Task Handle_WhenCurrentSetIsIncompleteOrUnsaved_AndNaturalVoicePhrasesUsed_NeverCallsProviderAndReturnsPrimeroCompletaLaSerieActual(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            // Inputs completados o incompletos en UI pero sin persistir mediante "Completar serie":
            AddIncompleteSet(session.Ejercicios.First(), setNumber: 1, completada: false, reps: 0, peso: 0);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, $"turn-voice-{Guid.NewGuid():N}", "audio", phrase, null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_RegressionBugReal_WhenActiveSessionAndCurrentSetHasEmptyOrIncompleteData_DoesNotStartRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            // Serie actual visible en pantalla pero sin datos válidos completados (vacíos / repeticiones 0)
            AddIncompleteSet(session.Ejercicios.First(), setNumber: 1, completada: false, reps: 0, peso: 0);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-regression-bug", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSetOneCompletedAndSetTwoPending_StartsPrescribedRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            _sessionRepository.Sessions[session.Id] = session;

            var before = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-before", "audio", "inicie el descanso", null), CancellationToken.None);
            CompleteSets(session.Ejercicios.First(), 1);
            var after = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-after", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(before.ClientAction);
            Assert.Equal("StartRestTimer", after.ClientAction?.Type);
            Assert.Equal(150, after.ClientAction?.DurationSeconds);
        }

        [Fact]
        public async Task Handle_WhenImmediatelyPreviousSetIsNotCompleted_DoesNotStartRest()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            // Solo existe la serie 2: Series.Count = 1 => la serie actual es la 2, pero la 1 no está completada.
            session.Ejercicios.First().RegistrarOActualizarSerie(2, 80, 8, 1, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-gap", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Primero completá la serie actual.", result.Text);
        }

        [Fact]
        public async Task Handle_WhenLastSetOfExerciseCompletedAndNextExerciseExists_UsesCompletedExerciseRest()
        {
            var session = CreateTwoExerciseSession(firstRest: 90, secondRest: 180);
            CompleteSets(session.Ejercicios.OrderBy(e => e.Orden).First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-boundary", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(90, result.ClientAction?.DurationSeconds);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenLastSetOfWholeSessionIsCompleted_DoesNotStartRest()
        {
            var session = CreateTwoExerciseSession(firstRest: 90, secondRest: 180);
            var ordered = session.Ejercicios.OrderBy(e => e.Orden).ToList();
            CompleteSets(ordered[0], 1);
            CompleteSets(ordered[1], 2);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-end", "audio", "inicie el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Ya completaste todas las series de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRestIsAlreadyActive_DoesNotRestartIt()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            CompleteSets(session.Ejercicios.First(), 1);
            _sessionRepository.Sessions[session.Id] = session;
            var snapshot = new ClientSnapshotInput(null, null, true, 82, DateTime.UtcNow);

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-active", "audio", "Iniciá el descanso", snapshot), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Contains("ya está activo", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenStopRestIsExplicit_ReturnsStopOnlyWhenActive()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var active = new ClientSnapshotInput(null, null, true, 60, DateTime.UtcNow);

            var stopped = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-stop", "audio", "Pará el descanso", active), CancellationToken.None);
            var inactive = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-stop-2", "audio", "Detené el descanso", null), CancellationToken.None);

            Assert.Equal("StopRestTimer", stopped.ClientAction?.Type);
            Assert.Null(inactive.ClientAction);
            Assert.Contains("No tenés", inactive.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenTerminaElDescansoAndTimerIsActive_StopsWithoutFallingIntoOtherMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var active = new ClientSnapshotInput(null, null, true, 60, DateTime.UtcNow);

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-termina", "audio", "terminá el descanso", active), CancellationToken.None);

            Assert.Equal("StopRestTimer", result.ClientAction?.Type);
            Assert.Equal("Descanso detenido.", result.Text);
            Assert.DoesNotContain("solo puedo registrar", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_RegressionExact_WhenTerminarDescansoAndTimerIsActive_StopsWithoutProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var active = new ClientSnapshotInput(null, null, true, 60, DateTime.UtcNow);

            var routing = _intentRouter.Route("Terminar descanso.");
            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-terminar-exact", "audio", "Terminar descanso.", active), CancellationToken.None);

            Assert.Equal(LiveCoachIntentKind.StopRestTimer, routing.Kind);
            Assert.Equal("Descanso detenido.", result.Text);
            Assert.Equal("StopRestTimer", result.ClientAction?.Type);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNoRestIsPrescribed_DoesNotReturnClientAction()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 0);
            CompleteSets(session.Ejercicios.First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-missing", "audio", "Empezá el descanso", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Contains("No tengo un tiempo", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRestDurationIsCustom_DoesNotMutateOrInvokeProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-custom", "audio", "Poné el descanso en 2 minutos", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Por ahora uso el tiempo de descanso indicado por la rutina.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("¿Cuánto descanso me queda?")]
        [InlineData("¿Está activo el descanso?")]
        [InlineData("¿Cuánto falta?")]
        public async Task Handle_WhenActiveRestIsQueried_AnswersDeterministicallyAndRemainsReadOnly(string question)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var snapshot = new ClientSnapshotInput(null, null, true, 60, DateTime.UtcNow);

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, $"turn-rest-query-{Guid.NewGuid():N}", "audio", question, snapshot), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Contains("segundos de descanso", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCapturedRestExpiredDuringProcessing_ReportsFinishedWithoutProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var snapshot = new ClientSnapshotInput(null, null, true, 2, DateTime.UtcNow.AddSeconds(-5));

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-expired", "audio", "¿Ya terminó?", snapshot), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("El descanso ya terminó.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPrescribedRestIsQueried_ReturnsPrescriptionWithoutProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso, 150);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(new RespondLiveCoachCommand(
                session.Id, "turn-rest-prescribed", "audio", "¿Qué descanso me toca?", null), CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Te tocan 150 segundos de descanso.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
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
                "¿Cómo debería encarar esta serie?",
                null);

            await Assert.ThrowsAsync<ExternalServiceException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        private static void CompleteSets(EjercicioSesion exercise, int count)
        {
            for (var number = 1; number <= count; number++)
            {
                exercise.RegistrarOActualizarSerie(number, 80, 8, 1, DateTime.UtcNow);
            }
        }

        private static void AddIncompleteSet(
            EjercicioSesion exercise,
            int setNumber,
            bool completada = false,
            int reps = 0,
            decimal peso = 0)
        {
            var serie = new SerieEntrenamiento();
            serie.ConfigurarNumeroSerie(setNumber);
            if (reps > 0)
            {
                serie.RegistrarRendimiento(peso, reps, null, DateTime.UtcNow);
            }

            if (completada)
            {
                typeof(SerieEntrenamiento)
                    .GetProperty("Completada", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(serie, true);
            }

            if (typeof(EjercicioSesion)
                .GetField("_series", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(exercise) is List<SerieEntrenamiento> list)
            {
                list.Add(serie);
            }
        }

        private static SesionEntrenamiento CreateTwoExerciseSession(int firstRest, int secondRest)
        {
            EjercicioSesion Build(string name, int orden, int series, int rest)
            {
                var exercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = name,
                    GrupoMuscularPrincipal = "Pecho",
                    Activo = true
                };
                var exerciseSession = new EjercicioSesion(
                    exercise.Id, orden, series, 8, 10, 1, 2, rest);
                typeof(EjercicioSesion)
                    .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(exerciseSession, exercise);
                return exerciseSession;
            }

            return new SesionEntrenamiento(
                TestUserId,
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                [Build("Press de banca", 1, 1, firstRest), Build("Remo", 2, 2, secondRest)]);
        }

        private static SesionEntrenamiento CreateSession(
            string userId,
            EstadoSesionEntrenamiento estado,
            int restSeconds = 120)
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
                descansoObjetivoSegundos: restSeconds,
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

        private async Task<PendingLiveCoachAction> CreatePendingActionAsync(SesionEntrenamiento session)
        {
            var response = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    $"turn-pending-{Guid.NewGuid():N}",
                    "audio",
                    "Hice 8 con 100 kilos, RIR uno",
                    null),
                CancellationToken.None);

            Assert.NotNull(response.PendingAction);
            return _pendingActionStore.Actions[response.PendingAction.ActionId];
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

        private sealed class FakePendingActionStore : ILiveCoachPendingActionStore
        {
            public Dictionary<Guid, PendingLiveCoachAction> Actions { get; } = [];

            public void Set(PendingLiveCoachAction action) => Actions[action.ActionId] = action;

            public PendingLiveCoachActionLookupResult FindActive(
                string userId,
                Guid sessionId,
                DateTime nowUtc)
            {
                var action = Actions.Values.FirstOrDefault(item =>
                    item.UserId == userId && item.SessionId == sessionId);
                if (action is null)
                    return new(PendingLiveCoachActionLookupStatus.NotFound);
                if (nowUtc > action.ExpiresAtUtc)
                {
                    Actions.Remove(action.ActionId);
                    return new(PendingLiveCoachActionLookupStatus.Expired);
                }
                return new(PendingLiveCoachActionLookupStatus.Active, action);
            }

            public PendingLiveCoachActionTakeResult Take(Guid actionId, string userId, DateTime nowUtc)
                => Remove(actionId, userId, nowUtc);

            public PendingLiveCoachActionTakeResult Cancel(Guid actionId, string userId, DateTime nowUtc)
                => Remove(actionId, userId, nowUtc);

            private PendingLiveCoachActionTakeResult Remove(Guid actionId, string userId, DateTime nowUtc)
            {
                if (!Actions.TryGetValue(actionId, out var action))
                    return new(PendingLiveCoachActionTakeStatus.NotFound);
                if (action.UserId != userId)
                    return new(PendingLiveCoachActionTakeStatus.NotOwned);
                if (nowUtc > action.ExpiresAtUtc)
                    return new(PendingLiveCoachActionTakeStatus.Expired);
                Actions.Remove(actionId);
                return new(PendingLiveCoachActionTakeStatus.Success, action);
            }
        }

        private sealed class FakePendingActionExecutor(FakePendingActionStore store)
            : ILiveCoachPendingActionExecutor
        {
            public int ConfirmCount { get; private set; }
            public int CancelCount { get; private set; }
            public bool ThrowConflict { get; set; }

            public Task<TrainingSessionDto> ConfirmAsync(Guid actionId, CancellationToken cancellationToken)
            {
                ConfirmCount++;
                if (ThrowConflict)
                {
                    store.Actions.Remove(actionId);
                    throw new ConflictException("stale");
                }

                var action = store.Actions[actionId];
                store.Actions.Remove(actionId);
                return Task.FromResult(new TrainingSessionDto(
                    action.SessionId,
                    null,
                    DateTime.UtcNow.Date,
                    DateTime.UtcNow,
                    null,
                    EstadoSesionEntrenamiento.EnCurso,
                    []));
            }

            public Task CancelAsync(Guid actionId, CancellationToken cancellationToken)
            {
                CancelCount++;
                store.Actions.Remove(actionId);
                return Task.CompletedTask;
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
            public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default)
            {
                if (Sessions.TryGetValue(sessionId, out var session) && session.UsuarioId == userId)
                    return Task.FromResult<SesionEntrenamiento?>(session);
                return Task.FromResult<SesionEntrenamiento?>(null);
            }
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
