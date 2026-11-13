using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.LiveCoach.Commands.RespondLiveCoach;
using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Clarifications;
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
        private readonly FakeClarificationStore _clarificationStore;

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
            _clarificationStore = new FakeClarificationStore();

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
                _clarificationStore,
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
        public async Task Handle_WhenSkipCurrentExerciseIsExplicit_CreatesPendingActionWithoutMutation()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            var current = session.Ejercicios.OrderBy(x => x.Orden).First();
            current.RegistrarOActualizarSerie(1, 80, 8, 2, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-skip",
                    "audio",
                    "saltá este ejercicio",
                    null),
                CancellationToken.None);

            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.SkipCurrentExercise, result.PendingAction.Type);
            Assert.Equal("Te quedan 2 series de Hip Thrust. ¿Querés saltarlo igual?", result.Text);
            Assert.Contains("Hip Thrust", result.PendingAction.Summary);
            Assert.False(current.Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigateNextRequested_UsesVisibleExerciseAndDoesNotInvokeProvider()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3), ("Curl Femoral", 3));
            _sessionRepository.Sessions[session.Id] = session;
            var visible = session.Ejercicios.OrderBy(x => x.Orden).First();

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-nav-next",
                    "audio",
                    "mostrame el siguiente ejercicio",
                    new ClientSnapshotInput(null, null, null, null, null, visible.Id)),
                CancellationToken.None);

            Assert.Equal("Mostrando Prensa.", result.Text);
            Assert.Equal("NavigateNextExercise", result.ClientAction?.Type);
            Assert.Equal(session.Ejercicios.OrderBy(x => x.Orden).ElementAt(1).Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigatePreviousRequested_UsesVisibleExercise()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            _sessionRepository.Sessions[session.Id] = session;
            var visible = session.Ejercicios.OrderBy(x => x.Orden).Last();

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-nav-previous",
                    "audio",
                    "volvé al ejercicio anterior",
                    new ClientSnapshotInput(null, null, null, null, null, visible.Id)),
                CancellationToken.None);

            Assert.Equal("Volviendo a Hip Thrust.", result.Text);
            Assert.Equal("NavigatePreviousExercise", result.ClientAction?.Type);
            Assert.Equal(session.Ejercicios.OrderBy(x => x.Orden).First().Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenReturnToCurrentRequested_NavigatesFromVisiblePreviewToAuthoritativeExercise()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            ordered[0].RegistrarOActualizarSerie(1, 80, 8, 2, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-return-current",
                    "audio",
                    "volvé al que me toca",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[1].Id)),
                CancellationToken.None);

            Assert.Equal("Volviendo a Hip Thrust.", result.Text);
            Assert.Equal("NavigateToExercise", result.ClientAction?.Type);
            Assert.Equal(ordered[0].Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenAlreadyViewingAuthoritativeExercise_DoesNotEmitNavigation()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            var current = session.Ejercicios.OrderBy(x => x.Orden).First();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-already-current",
                    "audio",
                    "mostrame el ejercicio actual",
                    new ClientSnapshotInput(null, null, null, null, null, current.Id)),
                CancellationToken.None);

            Assert.Equal("Ya estás en Hip Thrust, que es el ejercicio que te toca.", result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSessionHasNoPendingTarget_ReturnCurrentDoesNotNavigate()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 1), ("Curl Femoral", 1));
            foreach (var exercise in session.Ejercicios)
                CompleteSets(exercise, 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-complete-current", "audio", "volvé al que me toca", null),
                CancellationToken.None);

            Assert.Equal("Ya completaste todas las series de la sesión.", result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenFirstExerciseIsOmitted_ReturnCurrentUsesNextPendingExercise()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            session.OmitirEjercicio(ordered[0].Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-current-after-skip",
                    "audio",
                    "mostrame el que me toca",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[0].Id)),
                CancellationToken.None);

            Assert.Equal("Volviendo a Curl Femoral.", result.Text);
            Assert.Equal(ordered[1].Id, result.ClientAction?.ExerciseSessionId);
            Assert.True(ordered[0].Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCurrentExerciseQueried_ReturnsAuthoritativeExerciseAndSet()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            session.Ejercicios.OrderBy(x => x.Orden).First()
                .RegistrarOActualizarSerie(1, 80, 8, 2, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-query", "audio", "¿qué ejercicio me toca?", null),
                CancellationToken.None);

            Assert.Equal("Te toca Hip Thrust, serie 2.", result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("¿cuántas repeticiones me tocan?", LiveCoachIntentKind.CurrentRepetitionsQuery)]
        [InlineData("¿cuánto peso tengo que usar?", LiveCoachIntentKind.CurrentWeightQuery)]
        [InlineData("¿qué RIR me toca?", LiveCoachIntentKind.CurrentRirQuery)]
        [InlineData("¿cuánto descanso tengo?", LiveCoachIntentKind.CurrentRestQuery)]
        [InlineData("¿qué descanso me toca?", LiveCoachIntentKind.CurrentRestQuery)]
        [InlineData("¿qué me toca en esta serie?", LiveCoachIntentKind.CurrentPrescriptionQuery)]
        [InlineData("recordame la serie", LiveCoachIntentKind.CurrentPrescriptionQuery)]
        public void IntentRouter_WhenCurrentPrescriptionIsQueried_RoutesDeterministically(
            string question,
            LiveCoachIntentKind expected)
        {
            var routing = _intentRouter.Route(question);

            Assert.Equal(expected, routing.Kind);
        }

        [Theory]
        [InlineData("¿cuánto falta de descanso?", LiveCoachIntentKind.RestTimerQuery)]
        [InlineData("iniciá el descanso", LiveCoachIntentKind.StartRestTimer)]
        public void IntentRouter_WhenRestIntentDiffers_PreservesExistingRouting(
            string question,
            LiveCoachIntentKind expected)
        {
            var routing = _intentRouter.Route(question);

            Assert.Equal(expected, routing.Kind);
        }

        [Fact]
        public void IntentRouter_WhenCompletedRepetitionsAreQueried_DoesNotUseCurrentPrescription()
        {
            var routing = _intentRouter.Route("¿cuántas reps hice?");

            Assert.Equal(LiveCoachIntentKind.PreviousSetPerformance, routing.Kind);
        }

        [Theory]
        [InlineData("¿cuántas series llevo?", LiveCoachIntentKind.CurrentExerciseCompletedSets)]
        [InlineData("¿cuántas completé?", LiveCoachIntentKind.CurrentExerciseCompletedSets)]
        [InlineData("¿cómo voy con este ejercicio?", LiveCoachIntentKind.CurrentExerciseProgress)]
        [InlineData("¿ya terminé este ejercicio?", LiveCoachIntentKind.CurrentExerciseCompletion)]
        [InlineData("¿cuántas series me quedan?", LiveCoachIntentKind.RemainingSets)]
        [InlineData("¿cuántos ejercicios me quedan?", LiveCoachIntentKind.RemainingExercises)]
        [InlineData("¿cuánto me falta para terminar?", LiveCoachIntentKind.SessionProgress)]
        [InlineData("¿qué hice en la serie anterior?", LiveCoachIntentKind.PreviousSetPerformance)]
        [InlineData("¿cuántas repeticiones me tocan?", LiveCoachIntentKind.CurrentRepetitionsQuery)]
        [InlineData("¿cuántas repeticiones hice?", LiveCoachIntentKind.PreviousSetPerformance)]
        public void IntentRouter_WhenExerciseProgressIsQueried_SeparatesNearbyIntents(
            string question,
            LiveCoachIntentKind expected)
        {
            var routing = _intentRouter.Route(question);

            Assert.Equal(expected, routing.Kind);
        }

        [Theory]
        [InlineData(8, 8, "Te tocan 8 repeticiones.")]
        [InlineData(8, 10, "Te tocan entre 8 y 10 repeticiones.")]
        public async Task Handle_WhenCurrentRepetitionsQueried_ReturnsExactModelPrescription(
            int minRepetitions,
            int maxRepetitions,
            string expected)
        {
            var session = CreatePrescriptionSession(
                ("Hip Thrust", 3, minRepetitions, maxRepetitions, 2, 2, 120));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-current-reps",
                    "audio",
                    "¿cuántas repeticiones me tocan?",
                    null),
                CancellationToken.None);

            Assert.Equal(expected, result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCurrentWeightQueried_ReturnsNoExplicitTarget()
        {
            var session = CreatePrescriptionSession(("Hip Thrust", 3, 8, 8, 1, 1, 120));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-current-weight",
                    "audio",
                    "¿cuánto peso tengo que usar?",
                    null),
                CancellationToken.None);

            Assert.Equal("No tenés un peso objetivo definido para esta serie.", result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData(2, 2, "Te toca RIR 2.")]
        [InlineData(1, 2, "Te toca RIR entre 1 y 2.")]
        public async Task Handle_WhenCurrentRirQueried_ReturnsExactModelPrescription(
            int minRir,
            int maxRir,
            string expected)
        {
            var session = CreatePrescriptionSession(("Hip Thrust", 3, 8, 10, minRir, maxRir, 120));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-rir", "audio", "¿qué RIR me toca?", null),
                CancellationToken.None);

            Assert.Equal(expected, result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData(60, "Te toca descansar 1 minuto.")]
        [InlineData(90, "Te toca descansar 1 minuto y 30 segundos.")]
        [InlineData(120, "Te tocan 2 minutos de descanso.")]
        [InlineData(0, "Esta serie no tiene un descanso objetivo definido.")]
        public async Task Handle_WhenCurrentRestQueried_FormatsPrescribedDuration(
            int restSeconds,
            string expected)
        {
            var session = CreatePrescriptionSession(("Hip Thrust", 3, 8, 10, 1, 2, restSeconds));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-rest", "audio", "¿cuánto descanso tengo?", null),
                CancellationToken.None);

            Assert.Equal(expected, result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenFullCurrentPrescriptionQueried_IncludesOnlyPrescribedFields()
        {
            var session = CreatePrescriptionSession(("Hip Thrust", 3, 8, 8, 1, 1, 120));
            var exercise = session.Ejercicios.Single();
            exercise.RegistrarOActualizarSerie(1, 100, 8, 1, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-full", "audio", "¿qué me toca en esta serie?", null),
                CancellationToken.None);

            Assert.Equal(
                "Hip Thrust, serie 2: 8 repeticiones, RIR 1 y 2 minutos de descanso.",
                result.Text);
            Assert.DoesNotContain("kilo", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenVisibleExerciseDiffers_CurrentPrescriptionUsesAuthoritativeTarget()
        {
            var session = CreatePrescriptionSession(
                ("Hip Thrust", 3, 8, 8, 1, 1, 120),
                ("Curl Femoral", 3, 12, 12, 2, 2, 90));
            var ordered = session.Ejercicios.OrderBy(exercise => exercise.Orden).ToList();
            ordered[0].RegistrarOActualizarSerie(1, 100, 8, 1, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;
            var snapshot = new ClientSnapshotInput(
                ordered[1].EjercicioId,
                1,
                false,
                null,
                null,
                ordered[1].Id);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-current-visible-drift",
                    "audio",
                    "¿qué me toca en esta serie?",
                    snapshot),
                CancellationToken.None);

            Assert.StartsWith("Hip Thrust, serie 2:", result.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Curl Femoral", result.Text, StringComparison.Ordinal);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSessionHasNoPendingTarget_CurrentPrescriptionReportsCompletion()
        {
            var session = CreatePrescriptionSession(("Hip Thrust", 1, 8, 8, 1, 1, 120));
            session.Ejercicios.Single().RegistrarOActualizarSerie(1, 100, 8, 1, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-complete", "audio", "recordame la serie", null),
                CancellationToken.None);

            Assert.Equal("Ya completaste todas las series de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigateFirstRequested_NavigatesBySessionOrder()
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Press banca", 3, "Pecho"),
                ("Remo", 3, "Espalda"));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-first",
                    "audio",
                    "mostrame el primer ejercicio",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[1].Id)),
                CancellationToken.None);

            Assert.Equal("Mostrando Press banca.", result.Text);
            Assert.Equal(ordered[0].Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigateLastRequested_CanNavigateToOmittedExercise()
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Press banca", 3, "Pecho"),
                ("Remo", 3, "Espalda"));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            session.OmitirEjercicio(ordered[1].Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-last",
                    "audio",
                    "mostrame el último ejercicio",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[0].Id)),
                CancellationToken.None);

            Assert.Equal("Mostrando Remo.", result.Text);
            Assert.Equal(ordered[1].Id, result.ClientAction?.ExerciseSessionId);
            Assert.True(ordered[1].Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigateToMuscleGroupRequested_UsesPrimaryMuscleMetadata()
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Press banca", 3, "Pecho"),
                ("Remo con barra", 3, "Espalda"),
                ("Jalón al pecho", 3, "Espalda"));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-muscle",
                    "audio",
                    "mostrame un ejercicio de espalda",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[0].Id)),
                CancellationToken.None);

            Assert.Equal("Mostrando Remo con barra.", result.Text);
            Assert.Equal(ordered[1].Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData(true, "mostrame el primer ejercicio", "Este es el primer ejercicio de la sesión.")]
        [InlineData(false, "mostrame el último ejercicio", "Este es el último ejercicio de la sesión.")]
        public async Task Handle_WhenBoundaryExerciseAlreadyVisible_DoesNotNavigate(
            bool first,
            string requestText,
            string expectedText)
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Press banca", 3, "Pecho"),
                ("Remo", 3, "Espalda"));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            var visible = first ? ordered[0] : ordered[1];
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    $"turn-boundary-{first}",
                    "audio",
                    requestText,
                    new ClientSnapshotInput(null, null, null, null, null, visible.Id)),
                CancellationToken.None);

            Assert.Equal(expectedText, result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenVisibleExerciseAlreadyMatchesMuscleGroup_DoesNotNavigate()
        {
            var session = CreateMultiExerciseSessionWithMuscles(("Remo con barra", 3, "Espalda"));
            var visible = session.Ejercicios.Single();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-muscle-current",
                    "audio",
                    "mostrame un ejercicio de espalda",
                    new ClientSnapshotInput(null, null, null, null, null, visible.Id)),
                CancellationToken.None);

            Assert.Equal("Ya estás viendo Remo con barra, un ejercicio de espalda.", result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenMuscleMatchIsOmitted_NavigatesWithoutReactivatingIt()
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Press banca", 3, "Pecho"),
                ("Remo con barra", 3, "Espalda"));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            var omitted = ordered[1];
            session.OmitirEjercicio(omitted.Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-muscle-omitted",
                    "audio",
                    "mostrame un ejercicio de espalda",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[0].Id)),
                CancellationToken.None);

            Assert.Equal("Mostrando Remo con barra.", result.Text);
            Assert.Equal(omitted.Id, result.ClientAction?.ExerciseSessionId);
            Assert.True(omitted.Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("mostrame uno de femorales", "Curl femoral", "Isquiotibiales")]
        [InlineData("andá a uno de pantorrillas", "Elevación de gemelos", "Gemelos")]
        [InlineData("mostrame uno de piernas", "Sentadilla", "Cuádriceps")]
        public async Task Handle_WhenMuscleAliasRequested_UsesControlledMapping(
            string requestText,
            string exerciseName,
            string primaryMuscle)
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Press banca", 3, "Pecho"),
                (exerciseName, 3, primaryMuscle));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, $"turn-alias-{primaryMuscle}", "audio", requestText, null),
                CancellationToken.None);

            Assert.Equal(session.Ejercicios.OrderBy(x => x.Orden).Last().Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigateNextByMuscleRequested_UsesVisibleExerciseAsCursor()
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Remo con barra", 3, "Espalda"),
                ("Press banca", 3, "Pecho"),
                ("Jalón al pecho", 3, "Espalda"));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-next-muscle",
                    "audio",
                    "mostrame el siguiente ejercicio de espalda",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[0].Id)),
                CancellationToken.None);

            Assert.Equal("Mostrando el siguiente ejercicio de espalda: Jalón al pecho.", result.Text);
            Assert.Equal(ordered[2].Id, result.ClientAction?.ExerciseSessionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNoLaterMuscleMatchExists_DoesNotNavigate()
        {
            var session = CreateMultiExerciseSessionWithMuscles(
                ("Remo con barra", 3, "Espalda"),
                ("Press banca", 3, "Pecho"));
            var visible = session.Ejercicios.OrderBy(x => x.Orden).First();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-next-muscle-limit",
                    "audio",
                    "mostrame el siguiente de espalda",
                    new ClientSnapshotInput(null, null, null, null, null, visible.Id)),
                CancellationToken.None);

            Assert.Equal("No hay otro ejercicio de espalda después de este.", result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("mostrame un ejercicio de hombros", "No tenés ejercicios de hombros en esta sesión.")]
        [InlineData("mostrame uno de arriba", "¿Querés ver pecho, espalda, hombros o brazos?")]
        public async Task Handle_WhenMuscleGroupCannotResolve_DoesNotNavigate(
            string requestText,
            string expectedText)
        {
            var session = CreateMultiExerciseSessionWithMuscles(("Press banca", 3, "Pecho"));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-muscle-missing", "audio", requestText, null),
                CancellationToken.None);

            Assert.Equal(expectedText, result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNavigateByNameRequested_CanOpenOmittedExerciseWithoutReactivatingIt()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            var omitted = session.Ejercicios.OrderBy(x => x.Orden).First();
            session.OmitirEjercicio(omitted.Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-nav-name", "audio", "abrí hip thrust", null),
                CancellationToken.None);

            Assert.Equal("Mostrando Hip Thrust.", result.Text);
            Assert.Equal("NavigateToExercise", result.ClientAction?.Type);
            Assert.Equal(omitted.Id, result.ClientAction?.ExerciseSessionId);
            Assert.True(omitted.Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("mostrame remo", "No encuentro ese ejercicio en esta sesión.")]
        [InlineData("mostrame press", "Encontré más de un ejercicio parecido. ¿Cuál querés ver?")]
        public async Task Handle_WhenNavigateByNameCannotResolve_DoesNotNavigate(
            string requestText,
            string expectedText)
        {
            var session = CreateMultiExerciseSession(("Press Plano", 3), ("Press Inclinado", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-nav-unresolved", "audio", requestText, null),
                CancellationToken.None);

            Assert.Equal(expectedText, result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("volvé al ejercicio anterior", true, "Este es el primer ejercicio.")]
        [InlineData("mostrame el siguiente ejercicio", false, "Este es el último ejercicio.")]
        public async Task Handle_WhenNavigateRelativeHitsBoundary_ReturnsDeterministicMessage(
            string requestText,
            bool useFirst,
            string expectedText)
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            _sessionRepository.Sessions[session.Id] = session;
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            var visible = useFirst ? ordered.First() : ordered.Last();

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-nav-boundary",
                    "audio",
                    requestText,
                    new ClientSnapshotInput(null, null, null, null, null, visible.Id)),
                CancellationToken.None);

            Assert.Equal(expectedText, result.Text);
            Assert.Null(result.ClientAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSkipPendingActionIsCancelled_DoesNotMutateExercise()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            _sessionRepository.Sessions[session.Id] = session;
            var pending = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-skip", "audio", "saltá este ejercicio", null),
                CancellationToken.None);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-no", "audio", "no", null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.False(session.Ejercicios.First().Omitido);
            Assert.Equal(1, _pendingActionExecutor.CancelCount);
            Assert.DoesNotContain(pending.PendingAction!.ActionId, _pendingActionStore.Actions.Keys);
        }

        [Fact]
        public async Task Handle_WhenReactivateExerciseIsExplicit_CreatesPendingActionWithoutMutation()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            var hipThrust = session.Ejercicios.OrderBy(x => x.Orden).First();
            hipThrust.RegistrarOActualizarSerie(1, 80, 8, 2, DateTime.UtcNow);
            session.OmitirEjercicio(hipThrust.Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-reactivate", "audio", "reactivá hip thrust", null),
                CancellationToken.None);

            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.ReactivateExercise, result.PendingAction.Type);
            Assert.Equal("Reactivar Hip Thrust", result.PendingAction.Summary);
            Assert.Equal("Hip Thrust está omitido. ¿Querés reactivarlo?", result.Text);
            Assert.True(hipThrust.Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenReactivatePendingActionIsCancelled_KeepsExerciseOmitted()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            var exercise = session.Ejercicios.Single();
            session.OmitirEjercicio(exercise.Id);
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-reactivate", "audio", "reactivá hip thrust", null),
                CancellationToken.None);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-no-reactivate", "audio", "no", null),
                CancellationToken.None);

            Assert.Equal("Listo, lo dejo omitido.", result.Text);
            Assert.True(exercise.Omitido);
            Assert.Equal(1, _pendingActionExecutor.CancelCount);
        }

        [Fact]
        public async Task Handle_WhenNoExercisesAreOmitted_ReturnsDeterministicResponse()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-active", "audio", "reactivá hip thrust", null),
                CancellationToken.None);

            Assert.Equal("Hip Thrust ya está activo.", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNoExercisesAreOmittedAndNameDoesNotMatch_ReportsNoneAvailable()
        {
            var session = CreateMultiExerciseSession(("Prensa", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-none", "audio", "reactivá hip thrust", null),
                CancellationToken.None);

            Assert.Equal("No tenés ejercicios omitidos para reactivar.", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenReactivationNameIsNotFound_DoesNotCreatePendingAction()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Prensa", 3));
            session.OmitirEjercicio(session.Ejercicios.First().Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-unknown", "audio", "reactivá sentadilla", null),
                CancellationToken.None);

            Assert.Contains("No encontré", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenReactivationNameIsAmbiguous_AsksForClarification()
        {
            var session = CreateMultiExerciseSession(
                ("Press banca barra", 3),
                ("Press banca mancuernas", 3));
            foreach (var exercise in session.Ejercicios)
                session.OmitirEjercicio(exercise.Id);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-ambiguous", "audio", "reactivá press banca", null),
                CancellationToken.None);

            Assert.Contains("más de un ejercicio omitido parecido", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenReactivationConfirmationIsStale_DoesNotConfirm()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            session.OmitirEjercicio(session.Ejercicios.Single().Id);
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-reactivate", "audio", "reactivá hip thrust", null),
                CancellationToken.None);
            _pendingActionExecutor.ThrowConflict = true;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-confirm-stale", "audio", "sí", null),
                CancellationToken.None);

            Assert.Contains("La sesión cambió", result.Text);
            Assert.True(session.Ejercicios.Single().Omitido);
        }

        [Fact]
        public async Task Handle_WhenExerciseWasAlreadyReactivated_ReturnsSpecificResponse()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            session.OmitirEjercicio(session.Ejercicios.Single().Id);
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-reactivate", "audio", "reactivá hip thrust", null),
                CancellationToken.None);
            _pendingActionExecutor.ThrowConflict = true;
            _pendingActionExecutor.ConflictMessage = "El ejercicio no está omitido.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-confirm-active", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal("Ese ejercicio ya fue reactivado.", result.Text);
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
        public async Task Handle_WhenVoiceConfirmationStartsRest_ReturnsSharedClientActionAndText()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);
            _pendingActionExecutor.ClientActionToReturn =
                new LiveCoachClientActionDto("StartRestTimer", 150);
            _pendingActionExecutor.TextToReturn =
                "Serie registrada. Descanso: 150 segundos.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-confirm-rest",
                    "audio",
                    "sí",
                    new ClientSnapshotInput(
                        session.Ejercicios.Single().EjercicioId,
                        1,
                        RestIsActive: false,
                        RestRemainingSeconds: 0,
                        RestCapturedAtUtc: DateTime.UtcNow)),
                CancellationToken.None);

            Assert.Equal("Serie registrada. Descanso: 150 segundos.", result.Text);
            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(150, result.ClientAction?.DurationSeconds);
            Assert.False(_pendingActionExecutor.LastRestTimerIsActive);
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

        [Theory]
        [InlineData("En realidad fueron 9 reps.")]
        [InlineData("Eran 9 repeticiones")]
        [InlineData("En realidad hice 9")]
        [InlineData("Cambiá las reps a 9")]
        [InlineData("Poné 9 repeticiones")]
        public async Task Handle_WhenPendingActionExists_UpdatesOnlyRepetitions(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var original = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-reps", "audio", phrase, null),
                CancellationToken.None);

            var updated = _pendingActionStore.Actions[original.ActionId];
            Assert.Equal(original.ActionId, result.PendingAction?.ActionId);
            Assert.Equal(original.ExpiresAtUtc, updated.ExpiresAtUtc);
            Assert.Equal(9, updated.Repetitions);
            Assert.Equal(100m, updated.WeightKg);
            Assert.Equal(1, updated.Rir);
            Assert.Equal("100 kg × 9 · RIR 1", result.PendingAction?.Summary);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("Eran 95 kilos")]
        [InlineData("Poné 95 kilos")]
        [InlineData("Cambiá el peso a 95")]
        [InlineData("En realidad fueron 95 kg")]
        public async Task Handle_WhenPendingActionExists_UpdatesOnlyWeight(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var original = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-weight", "audio", phrase, null),
                CancellationToken.None);

            var updated = _pendingActionStore.Actions[original.ActionId];
            Assert.Equal(95m, updated.WeightKg);
            Assert.Equal(8, updated.Repetitions);
            Assert.Equal(1, updated.Rir);
            Assert.Equal("95 kg × 8 · RIR 1", result.PendingAction?.Summary);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("RIR dos")]
        [InlineData("Poné RIR 2")]
        [InlineData("Cambiá el RIR a dos")]
        [InlineData("En realidad RIR dos")]
        public async Task Handle_WhenPendingActionExists_UpdatesOnlyRir(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var original = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-rir", "audio", phrase, null),
                CancellationToken.None);

            var updated = _pendingActionStore.Actions[original.ActionId];
            Assert.Equal(100m, updated.WeightKg);
            Assert.Equal(8, updated.Repetitions);
            Assert.Equal(2, updated.Rir);
            Assert.Equal("100 kg × 8 · RIR 2", result.PendingAction?.Summary);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("En realidad fueron 9 con 95 kilos, RIR dos")]
        [InlineData("Cambiá a 95 kilos, 9 reps y RIR dos")]
        public async Task Handle_WhenPendingActionExists_UpdatesMultipleExplicitFields(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var original = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-all", "audio", phrase, null),
                CancellationToken.None);

            var updated = _pendingActionStore.Actions[original.ActionId];
            Assert.Equal(95m, updated.WeightKg);
            Assert.Equal(9, updated.Repetitions);
            Assert.Equal(2, updated.Rir);
            Assert.Equal("95 kg × 9 · RIR 2", result.PendingAction?.Summary);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("Poné 9")]
        [InlineData("Fueron 95")]
        public async Task Handle_WhenPendingUpdateIsAmbiguous_DoesNotChangeProposal(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var original = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-ambiguous", "audio", phrase, null),
                CancellationToken.None);

            Assert.Equal("¿Querés cambiar el peso, las repeticiones o el RIR?", result.Text);
            Assert.Equal(original, _pendingActionStore.Actions[original.ActionId]);
            Assert.Equal(original.ActionId, result.PendingAction?.ActionId);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("Poné -1 repeticiones", "repeticiones")]
        [InlineData("Poné 1001 kilos", "peso")]
        [InlineData("RIR 6", "RIR")]
        public async Task Handle_WhenPendingUpdateValueIsInvalid_DoesNotChangeProposal(
            string phrase,
            string expectedField)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var original = await CreatePendingActionAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-invalid", "audio", phrase, null),
                CancellationToken.None);

            Assert.Contains(expectedField, result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(original, _pendingActionStore.Actions[original.ActionId]);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("Fueron 9 reps")]
        [InlineData("Poné 95 kilos")]
        [InlineData("RIR dos")]
        public async Task Handle_WhenNoPendingActionExists_EditPhraseDoesNotCreateMutation(string phrase)
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-missing", "audio", phrase, null),
                CancellationToken.None);

            Assert.Contains("no hay una propuesta", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.PendingAction);
            Assert.Empty(_pendingActionStore.Actions);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPendingActionExpired_UpdateReturnsStaleWithoutMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var action = new PendingLiveCoachAction(
                Guid.NewGuid(), TestUserId, session.Id, session.Ejercicios.Single().Id,
                1, 100m, 8, 1, DateTime.UtcNow.AddSeconds(-1));
            _pendingActionStore.Set(action);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-expired", "audio", "Poné 95 kilos", null),
                CancellationToken.None);

            Assert.Contains("ya no está vigente", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.PendingAction);
            Assert.Empty(_pendingActionStore.Actions);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPendingTargetChanged_UpdateInvalidatesProposal()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);
            CompleteSets(session.Ejercicios.Single(), 1);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-stale", "audio", "Poné 95 kilos", null),
                CancellationToken.None);

            Assert.Contains("ya no está vigente", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.PendingAction);
            Assert.Empty(_pendingActionStore.Actions);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPendingActionIsUpdated_ConfirmationUsesUpdatedPayload()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);
            await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-before-confirm", "audio", "En realidad fueron 9 con 95 kilos, RIR dos", null),
                CancellationToken.None);

            var confirmed = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-confirm-updated", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal("Serie registrada.", confirmed.Text);
            Assert.Equal(95m, _pendingActionExecutor.ConfirmedAction?.WeightKg);
            Assert.Equal(9, _pendingActionExecutor.ConfirmedAction?.Repetitions);
            Assert.Equal(2, _pendingActionExecutor.ConfirmedAction?.Rir);
            Assert.Empty(_pendingActionStore.Actions);
        }

        [Fact]
        public async Task Handle_WhenPendingActionIsUpdated_CancellationStillRemovesItWithoutTrainingMutation()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            await CreatePendingActionAsync(session);
            await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-update-before-cancel", "audio", "RIR dos", null),
                CancellationToken.None);

            var canceled = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-cancel-updated", "audio", "no", null),
                CancellationToken.None);

            Assert.Equal("Listo, no la registro.", canceled.Text);
            Assert.Empty(_pendingActionStore.Actions);
            Assert.Empty(session.Ejercicios.Single().Series);
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
        [InlineData("Completame la serie actual, ciento veinte en peso ocho repeticiones, RIR uno", 8, 120, 1)]
        [InlineData("Completá la serie actual con 120 kilos, 8 reps y RIR 1", 8, 120, 1)]
        [InlineData("Registrame esta serie: 120 kilos, 8 repeticiones, RIR uno", 8, 120, 1)]
        [InlineData("Anotame 8 con 120 kilos, RIR uno", 8, 120, 1)]
        [InlineData("Cargá esta serie con peso 120, reps 8, RIR 1", 8, 120, 1)]
        [InlineData("Hice ocho con ciento veinte y RIR uno", 8, 120, 1)]
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
        public async Task Handle_RegressionLiteral_CompletameCurrentSetCreatesCorrectPendingActionWithoutProvider()
        {
            const string literal = "Completame la serie actual, ciento veinte en peso ocho repeticiones, RIR uno";
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var routing = _intentRouter.Route(literal);
            var extraction = _setActionExtractor.Extract(literal);
            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-register-literal", "audio", literal, null),
                CancellationToken.None);

            Assert.Equal(LiveCoachIntentKind.RegisterCurrentSet, routing.Kind);
            Assert.True(extraction.IsValid);
            Assert.Equal(120m, extraction.WeightKg);
            Assert.Equal(8, extraction.Repetitions);
            Assert.Equal(1, extraction.Rir);
            Assert.Equal("120 kg × 8 · RIR 1", result.PendingAction?.Summary);
            Assert.Equal(
                "Entendí: 120 kg por 8 repeticiones, RIR 1. ¿Querés que registre esta serie?",
                result.Text);
            Assert.Empty(session.Ejercicios.Single().Series);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCompleteCurrentSetHasNoPerformanceData_AsksForMissingDataWithoutProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-register-missing", "audio", "Completame la serie", null),
                CancellationToken.None);

            Assert.Contains("repeticiones", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.PendingAction);
            Assert.Empty(session.Ejercicios.Single().Series);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenOnlyBareNumbersAreProvided_DoesNotRegisterOrInvokeProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-register-ambiguous", "audio", "120, 8, 1", null),
                CancellationToken.None);

            Assert.Contains("peso", result.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Null(result.PendingAction);
            Assert.Empty(session.Ejercicios.Single().Series);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCompletingRoutineIsRequested_DoesNotRouteToRegisterCurrentSet()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var routing = _intentRouter.Route("Completame la rutina");
            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-complete-routine", "audio", "Completame la rutina", null),
                CancellationToken.None);

            Assert.Equal(LiveCoachIntentKind.OtherMutation, routing.Kind);
            Assert.Null(result.PendingAction);
            Assert.Empty(session.Ejercicios.Single().Series);
            Assert.Equal(0, _conversationProvider.CallCount);
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
            Assert.Equal("Te toca descansar 2 minutos y 30 segundos.", result.Text);
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
                "Contame sobre mi historial en este ejercicio",
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

        #region Performance Queries V1 Tests

        [Fact]
        public async Task Handle_WhenPreviousSetHasData_ReturnsFormattedSetAndDoesNotInvokeProvider()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.First();
            exercise.RegistrarOActualizarSerie(1, 120m, 8, 1, DateTime.UtcNow);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-prev-1",
                "audio",
                "¿Cuánto hice en la serie anterior?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("En la serie anterior hiciste 120 kg por 8 repeticiones, RIR 1.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
            Assert.True(_responseCache.TryGet(result.ResponseId, out var cached));
            Assert.Equal(result.Text, cached);
        }

        [Fact]
        public async Task Handle_WhenPreviousSetHasNoData_ReturnsNoSetsMessage()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-prev-0",
                "audio",
                "¿Qué hice en la serie anterior?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Todavía no completaste ninguna serie de este ejercicio.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPreviousSetRirQueried_WithRir_ReturnsRir()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.First();
            exercise.RegistrarOActualizarSerie(1, 100m, 10, 2, DateTime.UtcNow);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rir-1",
                "audio",
                "¿Qué RIR hice en la última serie?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("En la serie anterior registraste RIR 2.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPreviousSetRirQueried_WithoutRir_ReturnsNoRirRegistered()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.First();
            exercise.RegistrarOActualizarSerie(1, 100m, 10, null, DateTime.UtcNow);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rir-null",
                "audio",
                "¿Qué RIR hice en la última serie?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("En la serie anterior no registraste RIR.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPreviousSetRirQueried_WithoutSets_ReturnsNoSetsMessage()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rir-empty",
                "audio",
                "¿Qué RIR hice en la última?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Todavía no completaste ninguna serie de este ejercicio.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData(0, "Llevás 0 de 3 series en Hip Thrust.")]
        [InlineData(1, "Llevás 1 de 3 series en Hip Thrust.")]
        [InlineData(2, "Llevás 2 de 3 series en Hip Thrust.")]
        public async Task Handle_WhenCompletedSetsQueried_CountsOnlyAuthoritativeExercise(
            int completedSets,
            string expected)
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            CompleteSets(session.Ejercicios.OrderBy(x => x.Orden).First(), completedSets);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, $"turn-completed-{completedSets}", "audio", "¿Cuántas series llevo?", null),
                CancellationToken.None);

            Assert.Equal(expected, result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCurrentExerciseProgressQueried_ReturnsCompactProgress()
        {
            var session = CreateMultiExerciseSession(("Press banca", 4));
            CompleteSets(session.Ejercicios.Single(), 2);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-progress", "audio", "¿Cómo voy con este ejercicio?", null),
                CancellationToken.None);

            Assert.Equal("Llevás 2 de 4 series completadas en Press banca.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCurrentExerciseCompletionQueried_ReturnsRemainingSets()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            CompleteSets(session.Ejercicios.Single(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-current-completion", "audio", "¿Ya terminé este ejercicio?", null),
                CancellationToken.None);

            Assert.Equal("Todavía no. Te quedan 2 series de Hip Thrust.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCompletedSetsQueried_DoesNotCountPartialPayload()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            var exercise = session.Ejercicios.Single();
            AddIncompleteSet(exercise, 1, completada: true);
            exercise.RegistrarOActualizarSerie(2, 80, 8, 1, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-partial-set", "audio", "¿Cuántas completé?", null),
                CancellationToken.None);

            Assert.Equal("Llevás 1 de 3 series en Hip Thrust.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenEarlierExerciseIsOmitted_UsesNextPendingExerciseProgress()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            session.OmitirEjercicio(ordered[0].Id);
            CompleteSets(ordered[1], 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-omitted-progress", "audio", "¿Cuántas series llevo?", null),
                CancellationToken.None);

            Assert.Equal("Llevás 1 de 3 series en Curl Femoral.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenVisibleExerciseDiffers_RemainingSetsUseAuthoritativeExercise()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3), ("Curl Femoral", 3));
            var ordered = session.Ejercicios.OrderBy(x => x.Orden).ToList();
            CompleteSets(ordered[0], 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-visible-progress",
                    "audio",
                    "¿Cuántas series me quedan de este ejercicio?",
                    new ClientSnapshotInput(null, null, null, null, null, ordered[1].Id)),
                CancellationToken.None);

            Assert.Equal("Te quedan 2 series de Hip Thrust.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenExerciseAutomaticallyAdvances_CompletionQueryUsesNewAuthoritativeExercise()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 1), ("Curl Femoral", 2));
            CompleteSets(session.Ejercicios.OrderBy(x => x.Orden).First(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-after-advance", "audio", "¿Ya terminé este ejercicio?", null),
                CancellationToken.None);

            Assert.Equal("Todavía no. Te quedan 2 series de Curl Femoral.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSessionHasNoPendingTarget_ExerciseProgressReturnsSessionComplete()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 1));
            CompleteSets(session.Ejercicios.Single(), 1);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-progress-complete", "audio", "¿Cuántas series me quedan?", null),
                CancellationToken.None);

            Assert.Equal("Ya completaste todas las series de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRemainingSetsQueried_MultipleRemaining_ReturnsPluralCount()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.First(); // SeriesObjetivo = 4
            exercise.RegistrarOActualizarSerie(1, 80m, 8, 1, DateTime.UtcNow);
            exercise.RegistrarOActualizarSerie(2, 80m, 8, 1, DateTime.UtcNow);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rem-sets-2",
                "audio",
                "¿Cuántas series me faltan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Te quedan 2 series de Press de banca.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRemainingSetsQueried_OneRemaining_ReturnsSingularCount()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.First(); // SeriesObjetivo = 4
            exercise.RegistrarOActualizarSerie(1, 80m, 8, 1, DateTime.UtcNow);
            exercise.RegistrarOActualizarSerie(2, 80m, 8, 1, DateTime.UtcNow);
            exercise.RegistrarOActualizarSerie(3, 80m, 8, 1, DateTime.UtcNow);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rem-sets-1",
                "audio",
                "¿Cuántas series me faltan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Te queda 1 serie de Press de banca.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRemainingSetsQueried_AllCompleted_ReturnsAllCompletedMessage()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var exercise = session.Ejercicios.First(); // SeriesObjetivo = 4
            CompleteSets(exercise, 4);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rem-sets-0",
                "audio",
                "¿Cuántas series me faltan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Ya completaste todas las series de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPreviousExercisePerformance_WithHistory_ReturnsFormattedHistory()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;
            var targetExerciseId = session.Ejercicios.First().EjercicioId;

            _sessionRepository.Exposures[targetExerciseId] =
            [
                new ProgressExposure(
                    Guid.NewGuid(),
                    session.Id,
                    targetExerciseId,
                    DateTime.UtcNow.AddDays(-5),
                    new ProgressPrescriptionSnapshot(3, 8, 10, 1, 2, 120),
                    [
                        new ProgressCompletedSet(1, 100m, 8, 2),
                        new ProgressCompletedSet(2, 100m, 7, 2),
                        new ProgressCompletedSet(3, 95m, 8, 1)
                    ])
            ];

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-prev-hist",
                "audio",
                "¿Qué hice este ejercicio la última vez?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("La última vez hiciste 100 kg × 8, 100 kg × 7 y 95 kg × 8.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPreviousExercisePerformance_WithoutHistory_ReturnsNoHistoryMessage()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-no-hist",
                "audio",
                "¿Con cuánto hice este ejercicio la última vez?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("No tengo una sesión anterior registrada para este ejercicio.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNextExerciseQueried_HasNextExercise_ReturnsNextExerciseName()
        {
            var session = CreateTwoExerciseSession(90, 90);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-next",
                "audio",
                "¿Qué ejercicio sigue?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Después de Press de banca sigue Remo.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenNextExerciseQueried_OnLastExercise_ReturnsLastExerciseMessage()
        {
            var session = CreateTwoExerciseSession(90, 90);
            _sessionRepository.Sessions[session.Id] = session;
            CompleteSets(session.Ejercicios.First(), 1); // Exercise 1 done, now on exercise 2 (Remo, last)

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-next-last",
                "audio",
                "¿Qué ejercicio sigue?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Este es el último ejercicio de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRemainingExercisesQueried_MultipleRemaining_ReturnsPluralCount()
        {
            var session = CreateMultiExerciseSession(
                ("Press de banca", 3),
                ("Remo con barra", 3),
                ("Press militar", 3),
                ("Elevaciones laterales", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rem-ex-3",
                "audio",
                "¿Cuántos ejercicios me faltan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Te faltan 3 ejercicios después de este.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRemainingExercisesQueried_OneRemaining_ReturnsSingularCount()
        {
            var session = CreateTwoExerciseSession(90, 90);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rem-ex-1",
                "audio",
                "¿Cuántos ejercicios me faltan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Te falta 1 ejercicio después de este.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenRemainingExercisesQueried_OnLastExercise_ReturnsLastExerciseMessage()
        {
            var session = CreateTwoExerciseSession(90, 90);
            _sessionRepository.Sessions[session.Id] = session;
            CompleteSets(session.Ejercicios.First(), 1);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-rem-ex-0",
                "audio",
                "¿Cuántos ejercicios faltan?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Este es el último ejercicio de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSessionProgressQueried_SetsAndExercisesRemaining_ReturnsStructuralProgress()
        {
            var session = CreateMultiExerciseSession(
                ("Press de banca", 3),
                ("Remo con barra", 3),
                ("Press militar", 3),
                ("Elevaciones laterales", 3));
            _sessionRepository.Sessions[session.Id] = session;
            // Complete 1 set of press de banca: 2 sets remaining of this exercise, 3 exercises after this
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100m, 8, 1, DateTime.UtcNow);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-prog-struct",
                "audio",
                "¿Cuánto me falta para terminar?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Te faltan 2 series de este ejercicio y 3 ejercicios más.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSessionProgressQueried_OnlySetsRemainingOnLastExercise_ReturnsSetsOnly()
        {
            var session = CreateTwoExerciseSession(90, 90); // 1 set of press, 2 sets of remo
            _sessionRepository.Sessions[session.Id] = session;
            CompleteSets(session.Ejercicios.First(), 1); // Exercise 1 done, now on Remo (2 sets remaining)

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-prog-last-ex",
                "audio",
                "¿Cuánto me falta para terminar?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Te faltan 2 series de este ejercicio para terminar la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenSessionProgressQueried_AllCompleted_ReturnsAllCompleted()
        {
            var session = CreateTwoExerciseSession(90, 90);
            _sessionRepository.Sessions[session.Id] = session;
            CompleteSets(session.Ejercicios.First(), 1);
            CompleteSets(session.Ejercicios.Last(), 2);

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-prog-all-done",
                "audio",
                "¿Cuánto me falta para terminar?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Ya completaste todas las series de la sesión.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenBestSetTodayQueried_AsksForCriterion()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-best-set",
                "audio",
                "¿Cuál fue mi mejor serie de este ejercicio hoy?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result.Clarification);
            Assert.Equal("BestSetCriterion", result.Clarification.Kind);
            Assert.Collection(
                result.Clarification.Options,
                option =>
                {
                    Assert.Equal("peso", option.Value);
                    Assert.Equal("Peso", option.Label);
                },
                option =>
                {
                    Assert.Equal("repeticiones", option.Value);
                    Assert.Equal("Repeticiones", option.Label);
                },
                option =>
                {
                    Assert.Equal("volumen", option.Value);
                    Assert.Equal("Volumen", option.Label);
                });
            Assert.Equal("¿Querés comparar por peso, repeticiones o volumen?", result.Text);
            Assert.Single(_clarificationStore.Clarifications);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("peso", "Tu mejor serie por peso fue 8 repeticiones con 120 kilos, RIR 1.")]
        [InlineData("por peso", "Tu mejor serie por peso fue 8 repeticiones con 120 kilos, RIR 1.")]
        [InlineData("más peso", "Tu mejor serie por peso fue 8 repeticiones con 120 kilos, RIR 1.")]
        [InlineData("repeticiones", "Tu mejor serie por repeticiones fue 13 repeticiones con 80 kilos, RIR 2.")]
        [InlineData("reps", "Tu mejor serie por repeticiones fue 13 repeticiones con 80 kilos, RIR 2.")]
        [InlineData("por repeticiones", "Tu mejor serie por repeticiones fue 13 repeticiones con 80 kilos, RIR 2.")]
        [InlineData("volumen", "Tu mejor serie por volumen fue 10 repeticiones con 110 kilos.")]
        [InlineData("por volumen", "Tu mejor serie por volumen fue 10 repeticiones con 110 kilos.")]
        public async Task Handle_WhenBestSetCriterionFollowUpIsValid_ResolvesDeterministically(
            string criterion,
            string expected)
        {
            var session = CreateBestSetSession();
            _sessionRepository.Sessions[session.Id] = session;
            await AskBestSetAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, $"turn-best-{criterion}", "audio", criterion, null),
                CancellationToken.None);

            Assert.Equal(expected, result.Text);
            Assert.Null(result.Clarification);
            Assert.Empty(_clarificationStore.Clarifications);
            Assert.True(_responseCache.Cache.ContainsKey(result.ResponseId));
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenBestSetCriterionHasNoClarification_DoesNotTreatItAsBestSet()
        {
            var session = CreateBestSetSession();
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-weight-without-context", "audio", "peso", null),
                CancellationToken.None);

            Assert.Equal("Respuesta del coach.", result.Text);
            Assert.Equal(1, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenBestSetClarificationExpired_DoesNotApplyCriterion()
        {
            var session = CreateBestSetSession();
            _sessionRepository.Sessions[session.Id] = session;
            await AskBestSetAsync(session);
            _clarificationStore.ExpireAll();

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-best-expired", "audio", "peso", null),
                CancellationToken.None);

            Assert.Equal("Decime de nuevo qué querés comparar.", result.Text);
            Assert.Empty(_clarificationStore.Clarifications);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenExplicitIntentArrivesDuringClarification_ExecutesItAndClearsContext()
        {
            var session = CreateBestSetSession();
            _sessionRepository.Sessions[session.Id] = session;
            await AskBestSetAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-rest-during-best", "audio", "¿Cuánto descanso tengo?", null),
                CancellationToken.None);

            Assert.Equal("Te toca descansar 1 minuto y 30 segundos.", result.Text);
            Assert.Empty(_clarificationStore.Clarifications);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenAuthoritativeExerciseChangedDuringClarification_AsksToRepeat()
        {
            var session = CreateBestSetSession();
            _sessionRepository.Sessions[session.Id] = session;
            await AskBestSetAsync(session);
            session.Ejercicios.OrderBy(item => item.Orden).First()
                .RegistrarOActualizarSerie(4, 90, 8, 1, DateTime.UtcNow);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-best-stale", "audio", "peso", null),
                CancellationToken.None);

            Assert.Equal("La sesión cambió. Preguntame de nuevo cuál fue tu mejor serie.", result.Text);
            Assert.Empty(_clarificationStore.Clarifications);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenBestSetFollowUpUsesAnotherSession_DoesNotApplyOldContext()
        {
            var originalSession = CreateBestSetSession();
            var otherSession = CreateMultiExerciseSession(("Remo", 3));
            _sessionRepository.Sessions[originalSession.Id] = originalSession;
            _sessionRepository.Sessions[otherSession.Id] = otherSession;
            await AskBestSetAsync(originalSession);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(otherSession.Id, "turn-best-other-session", "audio", "peso", null),
                CancellationToken.None);

            Assert.Equal("Respuesta del coach.", result.Text);
            Assert.Equal(1, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenVisibleExerciseChangesDuringBestSetClarification_UsesCapturedExercise()
        {
            var session = CreateBestSetSession();
            var visibleExercise = session.Ejercicios.OrderBy(item => item.Orden).Last();
            _sessionRepository.Sessions[session.Id] = session;
            await AskBestSetAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-best-visible-different",
                    "audio",
                    "peso",
                    new ClientSnapshotInput(null, null, null, null, null, visibleExercise.Id)),
                CancellationToken.None);

            Assert.Equal("Tu mejor serie por peso fue 8 repeticiones con 120 kilos, RIR 1.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCapturedExerciseHasNoCompletedSets_ReturnsNaturalEmptyState()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await AskBestSetAsync(session);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-best-empty", "audio", "peso", null),
                CancellationToken.None);

            Assert.Equal("Aún no tenés series completadas de Hip Thrust hoy.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenCompareWithPreviousSessionQueried_AsksForCriterion()
        {
            var session = CreateSession(TestUserId, EstadoSesionEntrenamiento.EnCurso);
            _sessionRepository.Sessions[session.Id] = session;

            var command = new RespondLiveCoachCommand(
                session.Id,
                "turn-perf-compare-prev",
                "audio",
                "¿Estoy levantando más que la sesión anterior?",
                null);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("¿Querés comparar peso, repeticiones o volumen?", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        #endregion

        [Fact]
        public async Task Handle_WhenTimeConstraintNeedsCut_CreatesPendingAdaptationWithoutMutatingSession()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-time-adaptation",
                    "audio",
                    "Me quedan 8 minutos",
                    null),
                CancellationToken.None);

            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.ApplyTimeConstraintAdaptation, result.PendingAction.Type);
            var action = _pendingActionStore.Actions[result.PendingAction.ActionId];
            var proposal = Assert.IsType<LiveTimeConstraintProposal>(action.AdaptationProposal);
            Assert.Equal(8, proposal.RequestedRemainingMinutes);
            Assert.False(session.Ejercicios.Last().Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenTimeConstraintUsesWrittenNumber_CreatesDeterministicProposalWithoutProvider()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-time-adaptation-written",
                    "audio",
                    "Me quedan ocho minutos disponibles para entrenar",
                    null),
                CancellationToken.None);

            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.ApplyTimeConstraintAdaptation, result.PendingAction.Type);
            var proposal = Assert.IsType<LiveTimeConstraintProposal>(
                _pendingActionStore.Actions[result.PendingAction.ActionId].AdaptationProposal);
            Assert.Equal(8, proposal.RequestedRemainingMinutes);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenTimeAdaptationIsConfirmedByVoice_UsesPendingExecutor()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-time-pending", "audio", "Me quedan ocho minutos", null),
                CancellationToken.None);
            _pendingActionExecutor.TextToReturn =
                "Listo. Ajusté la sesión para acercarla a 8 minutos.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-time-confirm", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal(1, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(
                LiveCoachActionTypes.ApplyTimeConstraintAdaptation,
                _pendingActionExecutor.ConfirmedAction?.Type);
            Assert.NotNull(result.UpdatedSession);
            Assert.Equal("Listo. Ajusté la sesión para acercarla a 8 minutos.", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenTimeAdaptationIsCancelledByVoice_KeepsSessionUnchanged()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-time-pending-cancel", "audio", "Me quedan ocho minutos", null),
                CancellationToken.None);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-time-cancel", "audio", "no", null),
                CancellationToken.None);

            Assert.Equal("Listo, mantengo la sesión sin cambios.", result.Text);
            Assert.Equal(1, _pendingActionExecutor.CancelCount);
            Assert.All(session.Ejercicios, exercise => Assert.False(exercise.Omitido));
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenTimeAdaptationBecameStale_ReturnsSpecificMessage()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-time-pending-stale", "audio", "Me quedan ocho minutos", null),
                CancellationToken.None);
            _pendingActionExecutor.ThrowConflict = true;
            _pendingActionExecutor.ConflictMessage =
                "Tu sesión cambió desde que preparé el ajuste.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-time-stale", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal(
                "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo.",
                result.Text);
            Assert.Null(result.UpdatedSession);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData("estoy cansado", LiveFatigueSeverity.Moderate)]
        [InlineData("estoy muy cansado", LiveFatigueSeverity.High)]
        public async Task Handle_WhenFatigueIsExplicit_CreatesDeterministicPendingActionWithoutProvider(
            string text,
            LiveFatigueSeverity expectedSeverity)
        {
            var session = CreateMultiExerciseSession(
                ("Press", 3), ("Remo", 3), ("Curl", 3), ("Tríceps", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(session.Id, "turn-fatigue", "audio", text, null),
                CancellationToken.None);

            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.ApplyFatigueAdaptation, result.PendingAction.Type);
            var action = _pendingActionStore.Actions[result.PendingAction.ActionId];
            var proposal = Assert.IsType<LiveFatigueAdaptationProposal>(action.AdaptationProposal);
            Assert.Equal(expectedSeverity, proposal.FatigueSeverity);
            Assert.False(session.Ejercicios.Last().Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenFatigueAdaptationIsConfirmedByVoice_UsesPendingExecutor()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-pending", "audio", "estoy muy cansado", null),
                CancellationToken.None);
            _pendingActionExecutor.TextToReturn =
                "Listo. Ajusté el trabajo pendiente para esta sesión.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-confirm", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal(1, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(
                LiveCoachActionTypes.ApplyFatigueAdaptation,
                _pendingActionExecutor.ConfirmedAction?.Type);
            Assert.NotNull(result.UpdatedSession);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenFatigueAdaptationIsCancelledByVoice_KeepsSessionUnchanged()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-cancel-pending", "audio", "estoy cansado", null),
                CancellationToken.None);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-cancel", "audio", "no", null),
                CancellationToken.None);

            Assert.Equal("Listo, mantengo la sesión sin cambios.", result.Text);
            Assert.Equal(1, _pendingActionExecutor.CancelCount);
            Assert.All(session.Ejercicios, exercise => Assert.False(exercise.Omitido));
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenFatigueAdaptationBecameStale_ReturnsSpecificMessage()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-stale-pending", "audio", "estoy muy cansado", null),
                CancellationToken.None);
            _pendingActionExecutor.ThrowConflict = true;
            _pendingActionExecutor.ConflictMessage =
                "Tu sesión cambió desde que preparé el ajuste.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-stale", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal(
                "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo.",
                result.Text);
            Assert.Null(result.UpdatedSession);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenFatigueCannotReduce_ReturnsNaturalAnswerWithoutPendingAction()
        {
            var session = CreateMultiExerciseSession(("Press", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-fatigue-no-cut", "audio", "estoy cansado", null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.Contains("No hay más trabajo posterior", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPerformanceIsBelowPrescription_CreatesVolumeProposalWithoutProvider()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 6, 0, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance", "audio", "hoy estoy más débil", null),
                CancellationToken.None);

            Assert.NotNull(result.PendingAction);
            Assert.Equal(LiveCoachActionTypes.ApplyPerformanceAdaptation, result.PendingAction.Type);
            var action = _pendingActionStore.Actions[result.PendingAction.ActionId];
            var proposal = Assert.IsType<LivePerformanceAdaptationProposal>(action.AdaptationProposal);
            Assert.True(proposal.Evidence.MissedPrescription);
            Assert.False(session.Ejercicios.Last().Omitido);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenLastSetMetPrescription_DoesNotCreateAdaptation()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 8, 1, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-within", "audio", "hoy estoy más débil", null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.Contains("dentro de la prescripción", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenWeightFeelsHeavyWithoutEvidence_DoesNotInventTargetWeight()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-weight", "audio", "este peso está demasiado pesado", null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.Contains("No tengo un peso objetivo definido", result.Text);
            Assert.Contains("Registrá primero", result.Text);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPerformanceAdaptationIsConfirmedByVoice_UsesPendingExecutor()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 6, 0, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-pending", "audio", "no llego a las reps", null),
                CancellationToken.None);
            _pendingActionExecutor.TextToReturn =
                "Listo. Reduje el trabajo posterior de esta sesión.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-confirm", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal(1, _pendingActionExecutor.ConfirmCount);
            Assert.Equal(
                LiveCoachActionTypes.ApplyPerformanceAdaptation,
                _pendingActionExecutor.ConfirmedAction?.Type);
            Assert.NotNull(result.UpdatedSession);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPerformanceAdaptationIsCancelled_DoesNotMutateSession()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 6, 0, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-cancel-pending", "audio", "no llego a las reps", null),
                CancellationToken.None);

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-cancel", "audio", "no", null),
                CancellationToken.None);

            Assert.Equal("Listo, mantengo la sesión sin cambios.", result.Text);
            Assert.All(session.Ejercicios, exercise => Assert.False(exercise.Omitido));
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenPerformanceAdaptationBecameStale_ReturnsSpecificMessage()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            session.Ejercicios.First().RegistrarOActualizarSerie(1, 100, 6, 0, DateTime.UtcNow);
            _sessionRepository.Sessions[session.Id] = session;
            await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-stale-pending", "audio", "hoy estoy más débil", null),
                CancellationToken.None);
            _pendingActionExecutor.ThrowConflict = true;
            _pendingActionExecutor.ConflictMessage =
                "Tu sesión cambió desde que preparé el ajuste.";

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id, "turn-performance-stale", "audio", "sí", null),
                CancellationToken.None);

            Assert.Equal(
                "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo.",
                result.Text);
            Assert.Null(result.UpdatedSession);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Fact]
        public async Task Handle_WhenTimeConstraintAlreadyFits_DoesNotCreatePendingAction()
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    "turn-time-fits",
                    "text",
                    "Me quedan 20 minutos",
                    null),
                CancellationToken.None);

            Assert.Null(result.PendingAction);
            Assert.Contains("te alcanza", result.Text);
            Assert.Empty(_pendingActionStore.Actions);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(121)]
        public async Task Handle_WhenTimeConstraintIsOutsideRange_ReturnsNaturalValidation(int minutes)
        {
            var session = CreateMultiExerciseSession(("Press", 3), ("Remo", 3));
            _sessionRepository.Sessions[session.Id] = session;

            var result = await _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    $"turn-time-range-{minutes}",
                    "text",
                    $"Me quedan {minutes} minutos",
                    null),
                CancellationToken.None);

            Assert.Equal("Necesito un tiempo entre 5 y 120 minutos para ajustar la sesión.", result.Text);
            Assert.Null(result.PendingAction);
            Assert.Equal(0, _conversationProvider.CallCount);
        }

        private Task<LiveCoachRespondResponseDto> AskBestSetAsync(SesionEntrenamiento session)
        {
            return _handler.Handle(
                new RespondLiveCoachCommand(
                    session.Id,
                    $"turn-best-question-{Guid.NewGuid():N}",
                    "audio",
                    "¿Cuál fue mi mejor serie de este ejercicio hoy?",
                    null),
                CancellationToken.None);
        }

        private static SesionEntrenamiento CreateBestSetSession()
        {
            var session = CreateMultiExerciseSession(("Hip Thrust", 4), ("Curl Femoral", 3));
            var exercise = session.Ejercicios.OrderBy(item => item.Orden).First();
            exercise.RegistrarOActualizarSerie(1, 120, 8, 1, DateTime.UtcNow);
            exercise.RegistrarOActualizarSerie(2, 80, 13, 2, DateTime.UtcNow);
            exercise.RegistrarOActualizarSerie(3, 110, 10, null, DateTime.UtcNow);
            return session;
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

        private static SesionEntrenamiento CreateMultiExerciseSession(params (string Name, int Series)[] exercises)
        {
            var list = new List<EjercicioSesion>();
            for (var i = 0; i < exercises.Length; i++)
            {
                var (name, series) = exercises[i];
                var exercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = name,
                    GrupoMuscularPrincipal = "General",
                    Activo = true
                };
                var exerciseSession = new EjercicioSesion(
                    exercise.Id, i + 1, series, 8, 10, 1, 2, 90);
                typeof(EjercicioSesion)
                    .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(exerciseSession, exercise);
                list.Add(exerciseSession);
            }

            return new SesionEntrenamiento(
                TestUserId,
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                list);
        }

        private static SesionEntrenamiento CreateMultiExerciseSessionWithMuscles(
            params (string Name, int Series, string PrimaryMuscle)[] exercises)
        {
            var list = new List<EjercicioSesion>();
            for (var i = 0; i < exercises.Length; i++)
            {
                var (name, series, primaryMuscle) = exercises[i];
                var exercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = name,
                    GrupoMuscularPrincipal = primaryMuscle,
                    Activo = true
                };
                var exerciseSession = new EjercicioSesion(
                    exercise.Id, i + 1, series, 8, 10, 1, 2, 90);
                typeof(EjercicioSesion)
                    .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(exerciseSession, exercise);
                list.Add(exerciseSession);
            }

            return new SesionEntrenamiento(
                TestUserId,
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                list);
        }

        private static SesionEntrenamiento CreatePrescriptionSession(
            params (string Name, int Sets, int MinRepetitions, int MaxRepetitions, int MinRir, int MaxRir, int RestSeconds)[] prescriptions)
        {
            var exercises = new List<EjercicioSesion>();
            for (var index = 0; index < prescriptions.Length; index++)
            {
                var prescription = prescriptions[index];
                var exercise = new Ejercicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = prescription.Name,
                    GrupoMuscularPrincipal = "General",
                    Activo = true
                };
                var sessionExercise = new EjercicioSesion(
                    exercise.Id,
                    index + 1,
                    prescription.Sets,
                    prescription.MinRepetitions,
                    prescription.MaxRepetitions,
                    prescription.MinRir,
                    prescription.MaxRir,
                    prescription.RestSeconds);
                typeof(EjercicioSesion)
                    .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(sessionExercise, exercise);
                exercises.Add(sessionExercise);
            }

            return new SesionEntrenamiento(
                TestUserId,
                Guid.NewGuid(),
                DateTime.UtcNow.AddMinutes(-20),
                exercises);
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

            public PendingLiveCoachActionUpdateResult Update(
                Guid actionId,
                string userId,
                PendingLiveCoachSetPatch patch,
                DateTime nowUtc)
            {
                if (!Actions.TryGetValue(actionId, out var action))
                    return new(PendingLiveCoachActionUpdateStatus.NotFound);
                if (action.UserId != userId)
                    return new(PendingLiveCoachActionUpdateStatus.NotOwned);
                if (nowUtc > action.ExpiresAtUtc)
                {
                    Actions.Remove(actionId);
                    return new(PendingLiveCoachActionUpdateStatus.Expired);
                }

                var updated = action with
                {
                    WeightKg = patch.WeightKg ?? action.WeightKg,
                    Repetitions = patch.Repetitions ?? action.Repetitions,
                    Rir = patch.Rir ?? action.Rir
                };
                Actions[actionId] = updated;
                return new(PendingLiveCoachActionUpdateStatus.Success, updated);
            }

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

        private sealed class FakeClarificationStore : ILiveCoachClarificationStore
        {
            public Dictionary<Guid, PendingLiveCoachClarification> Clarifications { get; } = [];

            public void Set(PendingLiveCoachClarification clarification)
            {
                Clear(clarification.UserId, clarification.SessionId);
                Clarifications[clarification.ClarificationId] = clarification;
            }

            public PendingLiveCoachClarificationLookupResult FindActive(
                string userId,
                Guid sessionId,
                DateTime nowUtc)
            {
                var clarification = Clarifications.Values.FirstOrDefault(item =>
                    item.UserId == userId && item.SessionId == sessionId);
                if (clarification is null)
                    return new(PendingLiveCoachClarificationLookupStatus.NotFound);
                if (nowUtc > clarification.ExpiresAtUtc)
                {
                    Clarifications.Remove(clarification.ClarificationId);
                    return new(PendingLiveCoachClarificationLookupStatus.Expired);
                }

                return new(PendingLiveCoachClarificationLookupStatus.Active, clarification);
            }

            public PendingLiveCoachClarificationLookupResult TakeActive(
                string userId,
                Guid sessionId,
                DateTime nowUtc)
            {
                var lookup = FindActive(userId, sessionId, nowUtc);
                if (lookup.Status != PendingLiveCoachClarificationLookupStatus.Active ||
                    lookup.Clarification is null)
                {
                    return lookup;
                }

                Clarifications.Remove(lookup.Clarification.ClarificationId);
                return lookup;
            }

            public void Clear(string userId, Guid sessionId)
            {
                foreach (var clarification in Clarifications.Values
                    .Where(item => item.UserId == userId && item.SessionId == sessionId)
                    .ToList())
                {
                    Clarifications.Remove(clarification.ClarificationId);
                }
            }

            public void ExpireAll()
            {
                foreach (var clarification in Clarifications.Values.ToList())
                {
                    Clarifications[clarification.ClarificationId] = clarification with
                    {
                        ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1)
                    };
                }
            }
        }

        private sealed class FakePendingActionExecutor(FakePendingActionStore store)
            : ILiveCoachPendingActionExecutor
        {
            public int ConfirmCount { get; private set; }
            public int CancelCount { get; private set; }
            public bool ThrowConflict { get; set; }
            public string ConflictMessage { get; set; } = "stale";
            public PendingLiveCoachAction? ConfirmedAction { get; private set; }
            public bool LastRestTimerIsActive { get; private set; }
            public LiveCoachClientActionDto? ClientActionToReturn { get; set; }
            public string TextToReturn { get; set; } = "Serie registrada.";

            public Task<LiveCoachActionConfirmationDto> ConfirmAsync(
                Guid actionId,
                bool restTimerIsActive,
                CancellationToken cancellationToken)
            {
                ConfirmCount++;
                LastRestTimerIsActive = restTimerIsActive;
                if (ThrowConflict)
                {
                    store.Actions.Remove(actionId);
                    throw new ConflictException(ConflictMessage);
                }

                var action = store.Actions[actionId];
                ConfirmedAction = action;
                store.Actions.Remove(actionId);
                var session = new TrainingSessionDto(
                    action.SessionId,
                    null,
                    DateTime.UtcNow.Date,
                    DateTime.UtcNow,
                    null,
                    EstadoSesionEntrenamiento.EnCurso,
                    []);
                return Task.FromResult(new LiveCoachActionConfirmationDto(
                    session,
                    TextToReturn,
                    ClientActionToReturn));
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
