using System;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Prompt;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Application.Coach.LiveCoach.Rules;
using Entrenate.Application.Coach.LiveCoach.Tools;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Enums;
using Entrenate.Application.TrainingSessions;
using System.Globalization;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Entrenate.Application.Coach.LiveCoach.Commands.RespondLiveCoach
{
    public sealed class RespondLiveCoachCommandHandler
        : IRequestHandler<RespondLiveCoachCommand, LiveCoachRespondResponseDto>
    {
        private readonly ITrainingSessionRepository _sessionRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILiveCoachContextBuilder _contextBuilder;
        private readonly ILiveCoachToolRegistry _toolRegistry;
        private readonly ILiveCoachIntentRouter _intentRouter;
        private readonly ILiveCoachPromptBuilder _promptBuilder;
        private readonly ICoachConversationProvider _conversationProvider;
        private readonly ILiveCoachResponseCache _responseCache;
        private readonly ILiveCoachSetActionExtractor _setActionExtractor;
        private readonly ILiveCoachPendingActionStore _pendingActionStore;
        private readonly ILiveCoachPendingActionExecutor _pendingActionExecutor;
        private readonly ILogger<RespondLiveCoachCommandHandler> _logger;

        public RespondLiveCoachCommandHandler(
            ITrainingSessionRepository sessionRepository,
            ICurrentUserService currentUserService,
            ILiveCoachContextBuilder contextBuilder,
            ILiveCoachToolRegistry toolRegistry,
            ILiveCoachIntentRouter intentRouter,
            ILiveCoachPromptBuilder promptBuilder,
            ICoachConversationProvider conversationProvider,
            ILiveCoachResponseCache responseCache,
            ILiveCoachSetActionExtractor setActionExtractor,
            ILiveCoachPendingActionStore pendingActionStore,
            ILiveCoachPendingActionExecutor pendingActionExecutor,
            ILogger<RespondLiveCoachCommandHandler> logger)
        {
            _sessionRepository = sessionRepository;
            _currentUserService = currentUserService;
            _contextBuilder = contextBuilder;
            _toolRegistry = toolRegistry;
            _intentRouter = intentRouter;
            _promptBuilder = promptBuilder;
            _conversationProvider = conversationProvider;
            _responseCache = responseCache;
            _setActionExtractor = setActionExtractor;
            _pendingActionStore = pendingActionStore;
            _pendingActionExecutor = pendingActionExecutor;
            _logger = logger;
        }

        public async Task<LiveCoachRespondResponseDto> Handle(
            RespondLiveCoachCommand request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                throw new FluentValidation.ValidationException("El texto de la consulta es obligatorio.");
            }

            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            // 1. Authoritative session lookup
            var session = await _sessionRepository.GetByIdAndUserIdAsNoTrackingAsync(
                request.SessionId,
                userId,
                cancellationToken);

            if (session == null)
            {
                throw new NotFoundException("Sesión de entrenamiento no encontrada.");
            }

            if (session.Estado != EstadoSesionEntrenamiento.EnCurso)
            {
                throw new ConflictException("La sesión de entrenamiento no está activa.");
            }

            // 2. Pending-action state and deterministic intent routing
            var pendingLookup = _pendingActionStore.FindActive(
                userId,
                session.Id,
                DateTime.UtcNow);
            var activePendingAction = pendingLookup.Status == PendingLiveCoachActionLookupStatus.Active
                ? pendingLookup.Action
                : null;
            var routing = _intentRouter.Route(request.Text, activePendingAction is not null);

            if (routing.Kind == LiveCoachIntentKind.ConfirmPendingAction && activePendingAction is not null)
            {
                try
                {
                    var confirmation = await _pendingActionExecutor.ConfirmAsync(
                        activePendingAction.ActionId,
                        request.ClientSnapshot?.RestIsActive == true,
                        cancellationToken);
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        confirmation.Text,
                        updatedSession: confirmation.UpdatedSession,
                        clientAction: confirmation.ClientAction);
                }
                catch (ConflictException exception) when (
                    activePendingAction.Type == LiveCoachActionTypes.ReactivateExercise &&
                    exception.Message.Contains("no está omitido", StringComparison.OrdinalIgnoreCase))
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "Ese ejercicio ya fue reactivado.");
                }
                catch (Exception exception) when (exception is ConflictException or NotFoundException)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        activePendingAction.Type == LiveCoachActionTypes.SkipCurrentExercise
                            ? "La sesión cambió. Volvé a pedirme que salte el ejercicio."
                            : activePendingAction.Type == LiveCoachActionTypes.ReactivateExercise
                                ? "La sesión cambió. Volvé a pedirme que reactive el ejercicio."
                            : "La propuesta ya no está vigente. Decime nuevamente cómo salió la serie.");
                }
            }

            if (routing.Kind == LiveCoachIntentKind.CancelPendingAction && activePendingAction is not null)
            {
                try
                {
                    await _pendingActionExecutor.CancelAsync(
                        activePendingAction.ActionId,
                        cancellationToken);
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        activePendingAction.Type == LiveCoachActionTypes.SkipCurrentExercise
                            ? "Listo, seguimos con este ejercicio."
                            : activePendingAction.Type == LiveCoachActionTypes.ReactivateExercise
                                ? "Listo, lo dejo omitido."
                            : "Listo, no la registro.");
                }
                catch (Exception exception) when (exception is ConflictException or NotFoundException)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "La propuesta ya no está vigente. Decime nuevamente cómo salió la serie.");
                }
            }

            if (routing.Kind == LiveCoachIntentKind.UpdatePendingCurrentSet && activePendingAction is not null)
            {
                if (activePendingAction.Type != LiveCoachActionTypes.RegisterCurrentSet)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        activePendingAction.Type == LiveCoachActionTypes.ReactivateExercise
                            ? "¿Querés reactivarlo? Decime sí o no."
                            : "¿Querés saltarlo? Decime sí o no.",
                        ToDto(activePendingAction));
                }

                var patchExtraction = _setActionExtractor.ExtractPatch(request.Text);
                if (patchExtraction.Status == LiveCoachPendingSetPatchStatus.Invalid)
                {
                    var invalidText = patchExtraction.InvalidField switch
                    {
                        "rir" => "Ese valor de RIR no es válido.",
                        "repetitions" => "Ese valor de repeticiones no es válido.",
                        "weightKg" => "Ese valor de peso no es válido.",
                        _ => "Ese valor no es válido."
                    };
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        invalidText,
                        ToDto(activePendingAction));
                }

                if (patchExtraction.Status == LiveCoachPendingSetPatchStatus.Ambiguous ||
                    patchExtraction.Patch is null)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "¿Querés cambiar el peso, las repeticiones o el RIR?",
                        ToDto(activePendingAction));
                }

                var currentSet = CurrentPendingTrainingSetResolver.Resolve(session);
                if (currentSet is null ||
                    currentSet.ExerciseSession.Id != activePendingAction.ExerciseSessionId ||
                    currentSet.SetNumber != activePendingAction.SetNumber)
                {
                    _pendingActionStore.Cancel(activePendingAction.ActionId, userId, DateTime.UtcNow);
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "La propuesta ya no está vigente. Decime nuevamente cómo salió la serie.");
                }

                var update = _pendingActionStore.Update(
                    activePendingAction.ActionId,
                    userId,
                    patchExtraction.Patch,
                    DateTime.UtcNow);
                if (update.Status != PendingLiveCoachActionUpdateStatus.Success || update.Action is null)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "La propuesta ya no está vigente. Decime nuevamente cómo salió la serie.");
                }

                var updatedDto = ToDto(update.Action);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    $"Perfecto: {updatedDto.Summary}. ¿La registro?",
                    updatedDto);
            }

            if (routing.Kind == LiveCoachIntentKind.PendingActionUnavailable &&
                pendingLookup.Status == PendingLiveCoachActionLookupStatus.Expired)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    "La propuesta ya no está vigente. Decime nuevamente cómo salió la serie.");
            }

            if (routing.Kind == LiveCoachIntentKind.RegisterCurrentSet)
            {
                return CreateRegisterCurrentSetResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    request.Text);
            }

            if (routing.Kind == LiveCoachIntentKind.SkipCurrentExercise)
            {
                return CreateSkipCurrentExerciseResponse(
                    session,
                    userId,
                    request.ClientTurnId);
            }

            if (routing.Kind == LiveCoachIntentKind.ReactivateExercise)
            {
                return CreateReactivateExerciseResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    request.Text);
            }

            if (routing.Kind == LiveCoachIntentKind.ReturnToCurrentExercise)
            {
                return CreateReturnToCurrentExerciseResponse(
                    session,
                    request.ClientTurnId,
                    request.ClientSnapshot?.VisibleExerciseSessionId);
            }

            if (routing.Kind is LiveCoachIntentKind.NavigateFirstExercise or
                LiveCoachIntentKind.NavigateLastExercise or
                LiveCoachIntentKind.NavigateNextByMuscleGroup or
                LiveCoachIntentKind.NavigateToMuscleGroup)
            {
                return CreateSemanticExerciseNavigationResponse(
                    session,
                    request.ClientTurnId,
                    request.Text,
                    request.ClientSnapshot?.VisibleExerciseSessionId,
                    routing.Kind);
            }

            if (routing.Kind is LiveCoachIntentKind.NavigateNextExercise or
                LiveCoachIntentKind.NavigatePreviousExercise or
                LiveCoachIntentKind.NavigateToExercise)
            {
                return CreateExerciseNavigationResponse(
                    session,
                    request.ClientTurnId,
                    request.Text,
                    request.ClientSnapshot?.VisibleExerciseSessionId,
                    routing.Kind);
            }

            if (routing.Kind == LiveCoachIntentKind.PendingActionClarification &&
                activePendingAction?.Type == LiveCoachActionTypes.SkipCurrentExercise)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    "¿Querés saltarlo? Decime sí o no.",
                    ToDto(activePendingAction));
            }

            if (routing.Kind == LiveCoachIntentKind.PendingActionClarification &&
                activePendingAction?.Type == LiveCoachActionTypes.ReactivateExercise)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    "¿Querés reactivarlo? Decime sí o no.",
                    ToDto(activePendingAction));
            }

            if (!string.IsNullOrWhiteSpace(routing.DirectAnswer))
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    routing.DirectAnswer,
                    activePendingAction is null ? null : ToDto(activePendingAction));
            }

            // 3. Authorized context projection
            var nowUtc = DateTime.UtcNow;
            var context = _contextBuilder.Build(session, request.ClientSnapshot, nowUtc);

            if (routing.Kind is LiveCoachIntentKind.CurrentRepetitionsQuery or
                LiveCoachIntentKind.CurrentWeightQuery or
                LiveCoachIntentKind.CurrentRirQuery or
                LiveCoachIntentKind.CurrentRestQuery or
                LiveCoachIntentKind.CurrentPrescriptionQuery)
            {
                var prescription = _toolRegistry.GetCurrentPrescription(context).Data;
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachCurrentPrescriptionQueryResolver.Resolve(
                        routing.Kind,
                        context,
                        prescription));
            }

            if (routing.Kind == LiveCoachIntentKind.StartRestTimer)
            {
                if (context.Rest.IsActive)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        $"El descanso ya está activo. Te quedan {context.Rest.RemainingSecondsAtCapture ?? 0} segundos.");
                }

                var eligibility = LiveCoachRestEligibilityResolver.Resolve(session);
                if (eligibility.Status == LiveCoachRestEligibilityStatus.NoRecentlyCompletedSet)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "Primero completá la serie actual.");
                }

                if (eligibility.Status == LiveCoachRestEligibilityStatus.NoPendingSet)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "Ya completaste todas las series de la sesión.");
                }

                var durationSeconds = eligibility.RestSeconds;
                if (!durationSeconds.HasValue || durationSeconds.Value <= 0)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "No tengo un tiempo de descanso definido para esta serie.");
                }

                return CreateDirectResponse(
                    request.ClientTurnId,
                    $"Descanso iniciado: {durationSeconds.Value} segundos.",
                    clientAction: new LiveCoachClientActionDto("StartRestTimer", durationSeconds.Value));
            }

            if (routing.Kind == LiveCoachIntentKind.StopRestTimer)
            {
                if (!context.Rest.IsActive)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "No tenés un descanso activo.");
                }

                return CreateDirectResponse(
                    request.ClientTurnId,
                    "Descanso detenido.",
                    clientAction: new LiveCoachClientActionDto("StopRestTimer"));
            }

            if (routing.Kind == LiveCoachIntentKind.RestTimerQuery)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    CreateRestTimerQueryAnswer(context, request.Text));
            }

            if (routing.Kind == LiveCoachIntentKind.PreviousSetPerformance)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolvePreviousSetPerformance(session));
            }

            if (routing.Kind == LiveCoachIntentKind.PreviousSetRir)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolvePreviousSetRir(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExerciseCompletedSets)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCompletedSets(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExerciseProgress)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCurrentExerciseProgressSummary(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExerciseCompletion)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCurrentExerciseCompletion(session));
            }

            if (routing.Kind == LiveCoachIntentKind.RemainingSets)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveRemainingSets(session));
            }

            if (routing.Kind == LiveCoachIntentKind.BestSetToday)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveBestSetToday());
            }

            if (routing.Kind == LiveCoachIntentKind.PreviousExercisePerformance)
            {
                var answer = await LiveCoachPerformanceQueryResolver.ResolvePreviousExercisePerformanceAsync(
                    session,
                    userId,
                    _sessionRepository,
                    cancellationToken);

                return CreateDirectResponse(
                    request.ClientTurnId,
                    answer);
            }

            if (routing.Kind == LiveCoachIntentKind.CompareWithPreviousSession)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCompareWithPreviousSession());
            }

            if (routing.Kind == LiveCoachIntentKind.NextExercise)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveNextExercise(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExercise)
            {
                var current = CurrentPendingTrainingSetResolver.Resolve(session);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    current is null
                        ? "Ya completaste todas las series de la sesión."
                        : $"Te toca {current.ExerciseSession.Ejercicio.Nombre}, serie {current.SetNumber}.");
            }

            if (routing.Kind == LiveCoachIntentKind.RemainingExercises)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveRemainingExercises(session));
            }

            if (routing.Kind == LiveCoachIntentKind.SessionProgress)
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveSessionProgress(session));
            }

            // 4. Execute tool if requested by router
            PreviousPerformanceData? previousPerformance = null;
            if (routing.RequiresPreviousPerformance)
            {
                var perfResult = await _toolRegistry.GetPreviousPerformanceAsync(
                    context,
                    userId,
                    cancellationToken);

                previousPerformance = perfResult.Data;
            }

            // 5. Build prompt
            var prompt = _promptBuilder.Build(context, request.Text, previousPerformance);

            // 6. Invoke conversation provider
            string answerText;
            try
            {
                var providerResponse = await _conversationProvider.GenerateResponseAsync(
                    prompt,
                    cancellationToken);

                answerText = providerResponse?.Answer?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(answerText))
                {
                    answerText = "No pude generar una respuesta en este momento. Reintentá tu consulta.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Live Coach conversation provider failed for SessionId: {SessionId}",
                    request.SessionId);

                throw new ExternalServiceException("El servicio de Coach no está disponible en este momento.", ex);
            }

            // 7. Store in ephemeral cache for TTS coupling
            var responseId = Guid.NewGuid();
            _responseCache.Set(responseId, answerText, TimeSpan.FromSeconds(60));

            return new LiveCoachRespondResponseDto(
                responseId,
                request.ClientTurnId,
                answerText,
                context.IsStaleContext,
                true);
        }

        private LiveCoachRespondResponseDto CreateRegisterCurrentSetResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            string text)
        {
            var extraction = _setActionExtractor.Extract(text);
            if (!extraction.IsValid)
            {
                var missingText = extraction.MissingField switch
                {
                    "repetitions" => "Me faltaron las repeticiones. ¿Cuántas hiciste?",
                    "weightKg" => "Me faltó el peso. ¿Con cuántos kilos hiciste la serie?",
                    _ => "No pude interpretar la serie con seguridad. Indicame peso, repeticiones y RIR si corresponde."
                };

                return CreateDirectResponse(clientTurnId, missingText);
            }

            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "No encontré una serie pendiente en esta sesión.");
            }

            var actionId = Guid.NewGuid();
            var expiresAtUtc = DateTime.UtcNow.AddSeconds(45);
            var action = new PendingLiveCoachAction(
                actionId,
                userId,
                session.Id,
                current.ExerciseSession.Id,
                current.SetNumber,
                extraction.WeightKg!.Value,
                extraction.Repetitions!.Value,
                extraction.Rir,
                expiresAtUtc);

            _pendingActionStore.Set(action);

            var weight = extraction.WeightKg.Value.ToString("0.##", CultureInfo.InvariantCulture);
            var rirText = extraction.Rir.HasValue ? $", RIR {extraction.Rir.Value}" : string.Empty;
            var textResponse = $"Entendí: {weight} kg por {extraction.Repetitions.Value} repeticiones{rirText}. ¿Querés que registre esta serie?";
            var summary = $"{weight} kg × {extraction.Repetitions.Value}" +
                (extraction.Rir.HasValue ? $" · RIR {extraction.Rir.Value}" : string.Empty);

            var responseId = Guid.NewGuid();
            _responseCache.Set(responseId, textResponse, TimeSpan.FromSeconds(60));

            return new LiveCoachRespondResponseDto(
                responseId,
                clientTurnId,
                textResponse,
                false,
                true,
                new PendingLiveCoachActionDto(
                    actionId,
                    LiveCoachActionTypes.RegisterCurrentSet,
                    summary,
                    expiresAtUtc));
        }

        private LiveCoachRespondResponseDto CreateSkipCurrentExerciseResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId)
        {
            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
                return CreateDirectResponse(clientTurnId, "Ese ejercicio ya está completo.");

            var exercise = current.ExerciseSession;
            var completedTargetSets = exercise.Series
                .Where(x => x.EstaRealmenteCompletada())
                .Select(x => x.NumeroSerie)
                .Distinct()
                .Count(x => x >= 1 && x <= exercise.SeriesObjetivo);
            var pendingSetCount = Math.Max(0, exercise.SeriesObjetivo - completedTargetSets);
            if (pendingSetCount == 0)
                return CreateDirectResponse(clientTurnId, "Ese ejercicio ya está completo.");

            var exerciseName = exercise.Ejercicio.Nombre;
            var expiresAtUtc = DateTime.UtcNow.AddSeconds(45);
            var action = new PendingLiveCoachAction(
                Guid.NewGuid(),
                userId,
                session.Id,
                exercise.Id,
                current.SetNumber,
                0,
                0,
                null,
                expiresAtUtc,
                LiveCoachActionTypes.SkipCurrentExercise,
                exerciseName,
                pendingSetCount);
            _pendingActionStore.Set(action);

            var seriesLabel = pendingSetCount == 1 ? "serie" : "series";
            return CreateDirectResponse(
                clientTurnId,
                $"Te quedan {pendingSetCount} {seriesLabel} de {exerciseName}. ¿Querés saltarlo igual?",
                ToDto(action));
        }

        private LiveCoachRespondResponseDto CreateReactivateExerciseResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            string requestText)
        {
            var resolution = LiveCoachExerciseReactivationResolver.Resolve(session, requestText);
            if (resolution.Status == LiveCoachExerciseReactivationStatus.NoOmittedExercises)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "No tenés ejercicios omitidos para reactivar.");
            }

            if (resolution.Status == LiveCoachExerciseReactivationStatus.AlreadyActive)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"{resolution.Exercise!.Ejercicio.Nombre} ya está activo.");
            }

            if (resolution.Status == LiveCoachExerciseReactivationStatus.Ambiguous)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "Tenés más de un ejercicio omitido parecido. ¿Cuál querés reactivar?");
            }

            if (resolution.Status == LiveCoachExerciseReactivationStatus.NotFound ||
                resolution.Exercise is null)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "No encontré ese ejercicio entre los omitidos de esta sesión.");
            }

            var exercise = resolution.Exercise;
            var exerciseName = exercise.Ejercicio.Nombre;
            var expiresAtUtc = DateTime.UtcNow.AddSeconds(45);
            var action = new PendingLiveCoachAction(
                Guid.NewGuid(),
                userId,
                session.Id,
                exercise.Id,
                0,
                0,
                0,
                null,
                expiresAtUtc,
                LiveCoachActionTypes.ReactivateExercise,
                exerciseName);
            _pendingActionStore.Set(action);

            return CreateDirectResponse(
                clientTurnId,
                $"{exerciseName} está omitido. ¿Querés reactivarlo?",
                ToDto(action));
        }

        private LiveCoachRespondResponseDto CreateExerciseNavigationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string clientTurnId,
            string requestText,
            Guid? visibleExerciseSessionId,
            LiveCoachIntentKind intent)
        {
            var resolution = intent switch
            {
                LiveCoachIntentKind.NavigateNextExercise =>
                    LiveCoachExerciseNavigationResolver.ResolveRelative(session, visibleExerciseSessionId, 1),
                LiveCoachIntentKind.NavigatePreviousExercise =>
                    LiveCoachExerciseNavigationResolver.ResolveRelative(session, visibleExerciseSessionId, -1),
                _ => LiveCoachExerciseNavigationResolver.ResolveByName(session, requestText)
            };

            if (resolution.Status == LiveCoachExerciseNavigationStatus.FirstExercise)
                return CreateDirectResponse(clientTurnId, "Este es el primer ejercicio.");
            if (resolution.Status == LiveCoachExerciseNavigationStatus.LastExercise)
                return CreateDirectResponse(clientTurnId, "Este es el último ejercicio.");
            if (resolution.Status == LiveCoachExerciseNavigationStatus.Ambiguous)
                return CreateDirectResponse(clientTurnId, "Encontré más de un ejercicio parecido. ¿Cuál querés ver?");
            if (resolution.Status == LiveCoachExerciseNavigationStatus.NotFound || resolution.Exercise is null)
                return CreateDirectResponse(clientTurnId, "No encuentro ese ejercicio en esta sesión.");

            var actionType = intent.ToString();
            var prefix = intent == LiveCoachIntentKind.NavigatePreviousExercise ? "Volviendo a" : "Mostrando";
            return CreateDirectResponse(
                clientTurnId,
                $"{prefix} {resolution.Exercise.Ejercicio.Nombre}.",
                clientAction: new LiveCoachClientActionDto(
                    actionType,
                    ExerciseSessionId: resolution.Exercise.Id));
        }

        private LiveCoachRespondResponseDto CreateReturnToCurrentExerciseResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string clientTurnId,
            Guid? visibleExerciseSessionId)
        {
            var current = CurrentPendingTrainingSetResolver.Resolve(session);
            if (current is null)
                return CreateDirectResponse(clientTurnId, "Ya completaste todas las series de la sesión.");

            var exercise = current.ExerciseSession;
            if (visibleExerciseSessionId == exercise.Id)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"Ya estás en {exercise.Ejercicio.Nombre}, que es el ejercicio que te toca.");
            }

            return CreateDirectResponse(
                clientTurnId,
                $"Volviendo a {exercise.Ejercicio.Nombre}.",
                clientAction: new LiveCoachClientActionDto(
                    "NavigateToExercise",
                    ExerciseSessionId: exercise.Id));
        }

        private LiveCoachRespondResponseDto CreateSemanticExerciseNavigationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string clientTurnId,
            string requestText,
            Guid? visibleExerciseSessionId,
            LiveCoachIntentKind intent)
        {
            var resolution = intent switch
            {
                LiveCoachIntentKind.NavigateFirstExercise =>
                    LiveCoachExerciseNavigationResolver.ResolveBoundary(
                        session,
                        visibleExerciseSessionId,
                        first: true),
                LiveCoachIntentKind.NavigateLastExercise =>
                    LiveCoachExerciseNavigationResolver.ResolveBoundary(
                        session,
                        visibleExerciseSessionId,
                        first: false),
                LiveCoachIntentKind.NavigateNextByMuscleGroup =>
                    LiveCoachExerciseNavigationResolver.ResolveByMuscleGroup(
                        session,
                        requestText,
                        visibleExerciseSessionId,
                        nextOnly: true),
                _ => LiveCoachExerciseNavigationResolver.ResolveByMuscleGroup(
                    session,
                    requestText,
                    visibleExerciseSessionId,
                    nextOnly: false)
            };

            if (resolution.Status == LiveCoachExerciseNavigationStatus.FirstExercise)
                return CreateDirectResponse(clientTurnId, "Este es el primer ejercicio de la sesión.");
            if (resolution.Status == LiveCoachExerciseNavigationStatus.LastExercise)
                return CreateDirectResponse(clientTurnId, "Este es el último ejercicio de la sesión.");
            if (resolution.Status == LiveCoachExerciseNavigationStatus.UnknownMuscleGroup)
                return CreateDirectResponse(clientTurnId, "¿Querés ver pecho, espalda, hombros o brazos?");
            if (resolution.Status == LiveCoachExerciseNavigationStatus.NoNextMatch)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"No hay otro ejercicio de {resolution.MuscleGroupLabel} después de este.");
            }
            if (resolution.Status == LiveCoachExerciseNavigationStatus.AlreadyVisible &&
                resolution.Exercise is not null)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"Ya estás viendo {resolution.Exercise.Ejercicio.Nombre}, un ejercicio de {resolution.MuscleGroupLabel}.");
            }
            if (resolution.Status == LiveCoachExerciseNavigationStatus.NotFound)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    resolution.MuscleGroupLabel is null
                        ? "No encuentro ejercicios en esta sesión."
                        : $"No tenés ejercicios de {resolution.MuscleGroupLabel} en esta sesión.");
            }
            if (resolution.Exercise is null)
                return CreateDirectResponse(clientTurnId, "No encuentro ese ejercicio en esta sesión.");

            var text = intent == LiveCoachIntentKind.NavigateNextByMuscleGroup
                ? $"Mostrando el siguiente ejercicio de {resolution.MuscleGroupLabel}: {resolution.Exercise.Ejercicio.Nombre}."
                : $"Mostrando {resolution.Exercise.Ejercicio.Nombre}.";
            return CreateDirectResponse(
                clientTurnId,
                text,
                clientAction: new LiveCoachClientActionDto(
                    "NavigateToExercise",
                    ExerciseSessionId: resolution.Exercise.Id));
        }

        private LiveCoachRespondResponseDto CreateDirectResponse(
            string clientTurnId,
            string text,
            PendingLiveCoachActionDto? pendingAction = null,
            Entrenate.Application.TrainingSessions.DTOs.TrainingSessionDto? updatedSession = null,
            LiveCoachClientActionDto? clientAction = null)
        {
            var responseId = Guid.NewGuid();
            _responseCache.Set(responseId, text, TimeSpan.FromSeconds(60));
            return new LiveCoachRespondResponseDto(
                responseId,
                clientTurnId,
                text,
                false,
                true,
                pendingAction,
                updatedSession,
                clientAction);
        }

        private static PendingLiveCoachActionDto ToDto(PendingLiveCoachAction action)
        {
            return new PendingLiveCoachActionDto(
                action.ActionId,
                action.Type,
                BuildSummary(action),
                action.ExpiresAtUtc);
        }

        private static string BuildSummary(PendingLiveCoachAction action)
        {
            if (action.Type == LiveCoachActionTypes.SkipCurrentExercise)
            {
                var seriesLabel = action.PendingSetCount == 1
                    ? "serie pendiente"
                    : "series pendientes";
                return $"Saltarse {action.ExerciseName} con {action.PendingSetCount} {seriesLabel}";
            }

            if (action.Type == LiveCoachActionTypes.ReactivateExercise)
                return $"Reactivar {action.ExerciseName}";

            var weight = action.WeightKg.ToString("0.##", CultureInfo.InvariantCulture);
            return $"{weight} kg × {action.Repetitions}" +
                (action.Rir.HasValue ? $" · RIR {action.Rir.Value}" : string.Empty);
        }

        private static string CreateRestTimerQueryAnswer(
            Entrenate.Application.Coach.LiveCoach.Models.LiveCoachSessionContext context,
            string question)
        {
            var normalizedQuestion = RemoveDiacritics(question).ToLowerInvariant();
            var asksForPrescription = normalizedQuestion.Contains("toca", StringComparison.Ordinal) ||
                                      normalizedQuestion.Contains("debo descansar", StringComparison.Ordinal) ||
                                      normalizedQuestion.Contains("tengo que descansar", StringComparison.Ordinal) ||
                                      normalizedQuestion.Contains("que descanso", StringComparison.Ordinal);

            if (asksForPrescription)
            {
                var prescribedSeconds = context.CurrentPrescription?.RestSeconds;
                return prescribedSeconds.HasValue && prescribedSeconds.Value > 0
                    ? $"Te tocan {prescribedSeconds.Value} segundos de descanso."
                    : "No tenés un tiempo de descanso indicado para esta serie.";
            }

            if (context.Rest.IsActive)
            {
                var remainingSeconds = context.Rest.RemainingSecondsAtCapture ?? 0;
                return normalizedQuestion.Contains("activo", StringComparison.Ordinal) ||
                       normalizedQuestion.Contains("corriendo", StringComparison.Ordinal)
                    ? $"Sí. Te quedan {remainingSeconds} segundos de descanso."
                    : $"Te quedan {remainingSeconds} segundos de descanso.";
            }

            if (context.Rest.RemainingSecondsAtCapture == 0)
                return "El descanso ya terminó.";

            return "No tenés un descanso activo.";
        }

        private static string RemoveDiacritics(string text)
        {
            var decomposed = text.Normalize(NormalizationForm.FormD);
            var characters = new System.Text.StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    characters.Append(character);
            }

            return characters.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
