using System;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Clarifications;
using Entrenate.Application.Coach.LiveCoach.Conversation;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Prompt;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Application.Coach.LiveCoach.Rules;
using Entrenate.Application.Coach.LiveCoach.Tools;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Enums;
using Entrenate.Domain.Entidades;
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
        private readonly ILiveCoachClarificationStore _clarificationStore;
        private readonly ILiveCoachSessionConversationStore _sessionConversationStore;
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
            ILiveCoachClarificationStore clarificationStore,
            ILiveCoachSessionConversationStore sessionConversationStore,
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
            _clarificationStore = clarificationStore;
            _sessionConversationStore = sessionConversationStore;
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
                _sessionConversationStore.Clear(userId, session.Id);
                throw new ConflictException("La sesión de entrenamiento no está activa.");
            }

            // 2. Pending-action state and deterministic intent routing
            var routingNowUtc = DateTime.UtcNow;
            var sessionConversation = _sessionConversationStore.Get(
                userId,
                session.Id,
                routingNowUtc);
            var pendingLookup = _pendingActionStore.FindActive(
                userId,
                session.Id,
                routingNowUtc);
            var activePendingAction = pendingLookup.Status == PendingLiveCoachActionLookupStatus.Active
                ? pendingLookup.Action
                : null;
            var routing = _intentRouter.Route(request.Text, activePendingAction?.Type);
            var clarificationLookup = _clarificationStore.FindActive(userId, session.Id, routingNowUtc);
            var bestSetCriterion = LiveCoachBestSetClarificationResolver.ParseCriterion(
                RemoveDiacritics(request.Text).Trim().ToLowerInvariant());

            if (activePendingAction is null && bestSetCriterion.HasValue)
            {
                if (clarificationLookup.Status == PendingLiveCoachClarificationLookupStatus.Expired)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "Decime de nuevo qué querés comparar.");
                }

                if (clarificationLookup.Status == PendingLiveCoachClarificationLookupStatus.Active &&
                    clarificationLookup.Clarification is { } clarification &&
                    clarification.Kind == LiveCoachClarificationKind.BestSetCriterion &&
                    clarification.ValidCriteria.Contains(bestSetCriterion.Value))
                {
                    var taken = _clarificationStore.TakeActive(userId, session.Id, routingNowUtc);
                    if (taken.Status != PendingLiveCoachClarificationLookupStatus.Active ||
                        taken.Clarification is null)
                    {
                        return CreateDirectResponse(
                            request.ClientTurnId,
                            "Decime de nuevo qué querés comparar.");
                    }

                    var current = CurrentPendingTrainingSetResolver.Resolve(session);
                    var capturedExercise = session.Ejercicios.FirstOrDefault(
                        exercise => exercise.Id == taken.Clarification.ExerciseSessionId);
                    if (capturedExercise is null ||
                        capturedExercise.Omitido ||
                        current?.ExerciseSession.Id != capturedExercise.Id)
                    {
                        return CreateDirectResponse(
                            request.ClientTurnId,
                            "La sesión cambió. Preguntame de nuevo cuál fue tu mejor serie.");
                    }

                    RememberConversation(
                        userId,
                        session.Id,
                        context => context with
                        {
                            LastIntentKind = LiveCoachIntentKind.BestSetToday,
                            LastReferencedExerciseSessionId = capturedExercise.Id,
                            LastAuthoritativeExerciseSessionId = current.ExerciseSession.Id,
                            LastPerformanceQueryKind = LiveCoachIntentKind.BestSetToday,
                            LastBestSetCriterion = bestSetCriterion.Value
                        },
                        routingNowUtc);

                    return CreateDirectResponse(
                        request.ClientTurnId,
                        LiveCoachPerformanceQueryResolver.ResolveBestSetToday(
                            capturedExercise,
                            bestSetCriterion.Value));
                }
            }

            if (activePendingAction is null &&
                clarificationLookup.Status == PendingLiveCoachClarificationLookupStatus.Active &&
                routing.Kind == LiveCoachIntentKind.ConversationalQuestion)
            {
                return CreateBestSetClarificationResponse(request.ClientTurnId);
            }

            if (clarificationLookup.Status == PendingLiveCoachClarificationLookupStatus.Active)
                _clarificationStore.Clear(userId, session.Id);

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
                            : IsAdaptationAction(activePendingAction.Type)
                                ? GetAdaptationUnavailableText(exception)
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
                            : IsAdaptationAction(activePendingAction.Type)
                                ? "Listo, mantengo la sesión sin cambios."
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
                            : IsAdaptationAction(activePendingAction.Type)
                                ? "¿Querés aplicar el ajuste? Decime sí o no."
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
                    "Esa acción ya no está vigente porque venció. Pedímela de nuevo.");
            }

            if (activePendingAction is not null &&
                IsAdaptationAction(activePendingAction.Type) &&
                IsAdaptationIntent(routing.Kind))
            {
                _pendingActionStore.Cancel(
                    activePendingAction.ActionId,
                    userId,
                    routingNowUtc);
            }

            if (activePendingAction is not null &&
                IsAdaptationAction(activePendingAction.Type) &&
                sessionConversation is not null)
            {
                var adaptationFollowUp = LiveCoachSessionFollowUpResolver.Resolve(
                    request.Text,
                    sessionConversation);
                if (adaptationFollowUp?.Kind == LiveCoachSessionFollowUpKind.AdaptationChanges)
                {
                    return ResolveSessionFollowUp(
                        session,
                        userId,
                        request.ClientTurnId,
                        adaptationFollowUp,
                        sessionConversation,
                        pendingLookup,
                        routingNowUtc);
                }
            }

            if (routing.Kind is LiveCoachIntentKind.ConversationalQuestion or LiveCoachIntentKind.DataQuery &&
                activePendingAction is null &&
                sessionConversation is not null)
            {
                var followUp = LiveCoachSessionFollowUpResolver.Resolve(
                    request.Text,
                    sessionConversation);
                if (followUp is not null)
                {
                    return ResolveSessionFollowUp(
                        session,
                        userId,
                        request.ClientTurnId,
                        followUp,
                        sessionConversation,
                        pendingLookup,
                        routingNowUtc);
                }
            }

            if (routing.Kind is LiveCoachIntentKind.ConversationalQuestion or LiveCoachIntentKind.DataQuery &&
                activePendingAction is null &&
                sessionConversation is null &&
                LiveCoachSessionFollowUpResolver.IsPotentialFollowUp(request.Text))
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    "Necesito que me indiques de qué ejercicio o dato estás hablando.");
            }

            if (routing.Kind == LiveCoachIntentKind.RegisterCurrentSet)
            {
                return CreateRegisterCurrentSetResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    request.Text);
            }

            if (routing.Kind == LiveCoachIntentKind.LiveAdaptationTimeConstraint &&
                routing.RemainingMinutes.HasValue)
            {
                return CreateTimeConstraintAdaptationResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    routing.RemainingMinutes.Value);
            }

            if (routing.Kind == LiveCoachIntentKind.LiveAdaptationFatigueSignal &&
                routing.FatigueSeverity.HasValue)
            {
                return CreateFatigueAdaptationResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    routing.FatigueSeverity.Value);
            }

            if (routing.Kind == LiveCoachIntentKind.LiveAdaptationPerformanceSignal &&
                routing.PerformanceSignalType.HasValue)
            {
                return CreatePerformanceAdaptationResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    routing.PerformanceSignalType.Value);
            }

            if (routing.Kind == LiveCoachIntentKind.LiveAdaptationCombinedSignals)
            {
                return CreateCombinedAdaptationResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    new LiveAdaptationSignals(
                        routing.RemainingMinutes,
                        routing.FatigueSeverity,
                        routing.PerformanceSignalType));
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
                var response = CreateReturnToCurrentExerciseResponse(
                    session,
                    request.ClientTurnId,
                    request.ClientSnapshot?.VisibleExerciseSessionId);
                RememberNavigationResponse(
                    session,
                    userId,
                    routing.Kind,
                    response,
                    routingNowUtc);
                return response;
            }

            if (routing.Kind is LiveCoachIntentKind.NavigateFirstExercise or
                LiveCoachIntentKind.NavigateLastExercise or
                LiveCoachIntentKind.NavigateNextByMuscleGroup or
                LiveCoachIntentKind.NavigateToMuscleGroup)
            {
                var response = CreateSemanticExerciseNavigationResponse(
                    session,
                    request.ClientTurnId,
                    request.Text,
                    request.ClientSnapshot?.VisibleExerciseSessionId,
                    routing.Kind);
                RememberNavigationResponse(
                    session,
                    userId,
                    routing.Kind,
                    response,
                    routingNowUtc);
                return response;
            }

            if (routing.Kind is LiveCoachIntentKind.NavigateNextExercise or
                LiveCoachIntentKind.NavigatePreviousExercise or
                LiveCoachIntentKind.NavigateToExercise)
            {
                var response = CreateExerciseNavigationResponse(
                    session,
                    request.ClientTurnId,
                    request.Text,
                    request.ClientSnapshot?.VisibleExerciseSessionId,
                    routing.Kind);
                RememberNavigationResponse(
                    session,
                    userId,
                    routing.Kind,
                    response,
                    routingNowUtc);
                return response;
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

            if (routing.Kind == LiveCoachIntentKind.PendingActionClarification &&
                activePendingAction is not null &&
                IsAdaptationAction(activePendingAction.Type))
            {
                return CreateDirectResponse(
                    request.ClientTurnId,
                    "¿Querés aplicar el ajuste? Decime sí o no.",
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
                    clientAction: new LiveCoachClientActionDto(
                        "StartRestTimer",
                        durationSeconds.Value,
                        eligibility.ExerciseSessionId));
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
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.PreviousSetPerformance,
                    routingNowUtc,
                    performanceQuery: true);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolvePreviousSetPerformance(session));
            }

            if (routing.Kind == LiveCoachIntentKind.PreviousSetRir)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.PreviousSetRir,
                    routingNowUtc,
                    performanceQuery: true);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolvePreviousSetRir(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExerciseCompletedSets)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.CurrentExerciseCompletedSets,
                    routingNowUtc);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCompletedSets(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExerciseProgress)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.CurrentExerciseProgress,
                    routingNowUtc);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCurrentExerciseProgressSummary(session));
            }

            if (routing.Kind == LiveCoachIntentKind.CurrentExerciseCompletion)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.CurrentExerciseCompletion,
                    routingNowUtc);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveCurrentExerciseCompletion(session));
            }

            if (routing.Kind == LiveCoachIntentKind.RemainingSets)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.RemainingSets,
                    routingNowUtc);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveRemainingSets(session));
            }

            if (routing.Kind == LiveCoachIntentKind.BestSetToday)
            {
                var current = CurrentPendingTrainingSetResolver.Resolve(session);
                if (current is null)
                {
                    return CreateDirectResponse(
                        request.ClientTurnId,
                        "Ya completaste todas las series de la sesión.");
                }

                _clarificationStore.Set(new PendingLiveCoachClarification(
                    Guid.NewGuid(),
                    userId,
                    session.Id,
                    request.ClientTurnId,
                    current.ExerciseSession.Id,
                    LiveCoachClarificationKind.BestSetCriterion,
                    [
                        LiveCoachBestSetCriterion.Weight,
                        LiveCoachBestSetCriterion.Repetitions,
                        LiveCoachBestSetCriterion.Volume
                    ],
                    DateTime.UtcNow.AddSeconds(45)));

                RememberConversation(
                    userId,
                    session.Id,
                    context => context with
                    {
                        LastIntentKind = LiveCoachIntentKind.BestSetToday,
                        LastReferencedExerciseSessionId = current.ExerciseSession.Id,
                        LastAuthoritativeExerciseSessionId = current.ExerciseSession.Id,
                        LastPerformanceQueryKind = LiveCoachIntentKind.BestSetToday
                    },
                    routingNowUtc);

                return CreateBestSetClarificationResponse(request.ClientTurnId);
            }

            if (routing.Kind == LiveCoachIntentKind.PreviousExercisePerformance)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.PreviousExercisePerformance,
                    routingNowUtc,
                    performanceQuery: true);
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
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.CompareWithPreviousSession,
                    routingNowUtc,
                    performanceQuery: true);
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
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.CurrentExercise,
                    routingNowUtc);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    current is null
                        ? "Ya completaste todas las series de la sesión."
                        : $"Te toca {current.ExerciseSession.Ejercicio.Nombre}, serie {current.SetNumber}.");
            }

            if (routing.Kind == LiveCoachIntentKind.RemainingExercises)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.RemainingExercises,
                    routingNowUtc);
                return CreateDirectResponse(
                    request.ClientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveRemainingExercises(session));
            }

            if (routing.Kind == LiveCoachIntentKind.SessionProgress)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.SessionProgress,
                    routingNowUtc);
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

        private LiveCoachRespondResponseDto CreateTimeConstraintAdaptationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            int remainingMinutes)
        {
            if (remainingMinutes is < LiveTimeConstraintAdaptationEngine.MinimumRemainingMinutes or
                > LiveTimeConstraintAdaptationEngine.MaximumRemainingMinutes)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "Necesito un tiempo entre 5 y 120 minutos para ajustar la sesión.");
            }

            RememberTimeConstraint(userId, session.Id, remainingMinutes, DateTime.UtcNow);

            var result = LiveTimeConstraintAdaptationEngine.Evaluate(session, remainingMinutes);
            if (result.Status == LiveTimeConstraintResultStatus.NoPendingWork)
                return CreateDirectResponse(clientTurnId, "Ya completaste todas las series de la sesión.");
            if (result.Status == LiveTimeConstraintResultStatus.FitsAvailableTime)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"Con {remainingMinutes} minutos te alcanza para completar lo pendiente según la estimación actual.");
            }
            if (result.Status == LiveTimeConstraintResultStatus.CannotAdapt)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"Con {remainingMinutes} minutos no puedo ajustar la sesión sin recortar el ejercicio que estás haciendo.");
            }

            var nowUtc = DateTime.UtcNow;
            var expiresAtUtc = nowUtc.AddSeconds(45);
            var actionId = Guid.NewGuid();
            var proposal = new LiveTimeConstraintProposal(
                actionId,
                userId,
                session.Id,
                nowUtc,
                expiresAtUtc,
                remainingMinutes,
                result.EstimatedMinutesBefore,
                result.EstimatedMinutesAfter,
                LiveTimeConstraintFingerprint.Create(session),
                result.Changes,
                $"Ajustar sesión a ~{remainingMinutes} minutos");
            var current = CurrentPendingTrainingSetResolver.Resolve(session)!;
            var action = new PendingLiveCoachAction(
                actionId,
                userId,
                session.Id,
                current.ExerciseSession.Id,
                current.SetNumber,
                0,
                0,
                null,
                expiresAtUtc,
                LiveCoachActionTypes.ApplyTimeConstraintAdaptation,
                AdaptationProposal: proposal);
            _pendingActionStore.Set(action);
            RememberAdaptation(
                userId,
                session.Id,
                proposal.ProposalId,
                proposal.ExpiresAtUtc,
                proposal.Changes,
                [LiveAdaptationReason.TimeConstraint]);

            var omittedNames = FormatNaturalList(result.Changes.Select(change => change.ExerciseName));
            var reachesTarget = result.EstimatedMinutesAfter <= remainingMinutes;
            var text = reachesTarget
                ? $"Para acercarnos a {remainingMinutes} minutos puedo mantener {current.ExerciseSession.Ejercicio.Nombre} y omitir {omittedNames}. Lo pendiente pasaría de ~{result.EstimatedMinutesBefore} a ~{result.EstimatedMinutesAfter} minutos. ¿Querés aplicar este ajuste?"
                : $"Sin recortar {current.ExerciseSession.Ejercicio.Nombre}, puedo omitir {omittedNames}. Lo pendiente pasaría de ~{result.EstimatedMinutesBefore} a ~{result.EstimatedMinutesAfter} minutos. ¿Querés aplicar este ajuste?";

            return CreateDirectResponse(clientTurnId, text, ToDto(action));
        }

        private LiveCoachRespondResponseDto CreateFatigueAdaptationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            LiveFatigueSeverity severity)
        {
            var result = LiveFatigueAdaptationEngine.Evaluate(session, severity);
            if (result.Status == LiveFatigueAdaptationResultStatus.NoPendingWork)
                return CreateDirectResponse(clientTurnId, "Ya completaste todas las series de la sesión.");
            if (result.Status == LiveFatigueAdaptationResultStatus.NothingToReduce)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "Solo te queda el ejercicio que estás haciendo. No hay más trabajo posterior para recortar.");
            }

            var nowUtc = DateTime.UtcNow;
            var expiresAtUtc = nowUtc.AddSeconds(45);
            var actionId = Guid.NewGuid();
            var publicSummary = severity == LiveFatigueSeverity.High
                ? "Reducir trabajo pendiente por fatiga alta"
                : "Ajustar trabajo pendiente por cansancio";
            var proposal = new LiveFatigueAdaptationProposal(
                actionId,
                userId,
                session.Id,
                nowUtc,
                expiresAtUtc,
                severity,
                LiveTimeConstraintFingerprint.Create(session),
                result.Changes,
                publicSummary);
            var current = CurrentPendingTrainingSetResolver.Resolve(session)!;
            var action = new PendingLiveCoachAction(
                actionId,
                userId,
                session.Id,
                current.ExerciseSession.Id,
                current.SetNumber,
                0,
                0,
                null,
                expiresAtUtc,
                LiveCoachActionTypes.ApplyFatigueAdaptation,
                AdaptationProposal: proposal);
            _pendingActionStore.Set(action);
            RememberAdaptation(
                userId,
                session.Id,
                proposal.ProposalId,
                proposal.ExpiresAtUtc,
                proposal.Changes,
                [LiveAdaptationReason.Fatigue]);

            var omittedNames = FormatNaturalList(result.Changes.Select(change => change.ExerciseName));
            var text = severity == LiveFatigueSeverity.High
                ? $"Como me dijiste que estás muy cansado, puedo mantener el ejercicio actual y omitir {omittedNames}. ¿Querés aplicar este ajuste?"
                : $"Como me dijiste que estás cansado, puedo mantener {result.CurrentExerciseName} y omitir {omittedNames}. ¿Querés aplicar este ajuste?";

            return CreateDirectResponse(clientTurnId, text, ToDto(action));
        }

        private LiveCoachRespondResponseDto CreatePerformanceAdaptationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            LivePerformanceSignalType signalType)
        {
            var result = LivePerformanceAdaptationEngine.Evaluate(session, signalType);
            if (result.Status == LivePerformanceAdaptationResultStatus.NoPendingWork)
                return CreateDirectResponse(clientTurnId, "Ya completaste todas las series de la sesión.");

            if (result.Status == LivePerformanceAdaptationResultStatus.InsufficientContext)
            {
                var responseText = signalType == LivePerformanceSignalType.ExcessiveEffort
                    ? "No tengo un peso objetivo definido para esta serie y todavía no hay una serie reciente para comparar. Registrá primero cómo salió y la evalúo contra la prescripción."
                    : "Registrá primero cómo salió la serie y la comparo con la prescripción.";
                return CreateDirectResponse(clientTurnId, responseText);
            }

            var evidence = result.Evidence!;
            var evidenceText = FormatPerformanceEvidence(evidence);
            var weightLimitation = signalType == LivePerformanceSignalType.ExcessiveEffort
                ? "No tengo un peso objetivo definido para ajustar la carga automáticamente. "
                : string.Empty;
            if (result.Status == LivePerformanceAdaptationResultStatus.WithinPrescription)
            {
                var responseText = signalType == LivePerformanceSignalType.ExcessiveEffort
                    ? $"{weightLimitation}{evidenceText} Si querés, seguimos con la serie actual y evaluamos la próxima."
                    : $"{evidenceText} Si querés, seguimos con la serie actual y evaluamos la próxima.";
                return CreateDirectResponse(clientTurnId, responseText);
            }

            if (result.Status == LivePerformanceAdaptationResultStatus.UnderperformanceWithoutSafeCut)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    $"{weightLimitation}{evidenceText} No queda un ejercicio posterior no iniciado que pueda recortar sin tocar el trabajo actual o ya empezado.");
            }

            var nowUtc = DateTime.UtcNow;
            var expiresAtUtc = nowUtc.AddSeconds(45);
            var actionId = Guid.NewGuid();
            var proposal = new LivePerformanceAdaptationProposal(
                actionId,
                userId,
                session.Id,
                nowUtc,
                expiresAtUtc,
                signalType,
                evidence,
                LiveTimeConstraintFingerprint.Create(session),
                result.Changes,
                "Reducir volumen posterior por rendimiento bajo");
            var current = CurrentPendingTrainingSetResolver.Resolve(session)!;
            var action = new PendingLiveCoachAction(
                actionId,
                userId,
                session.Id,
                current.ExerciseSession.Id,
                current.SetNumber,
                0,
                0,
                null,
                expiresAtUtc,
                LiveCoachActionTypes.ApplyPerformanceAdaptation,
                AdaptationProposal: proposal);
            _pendingActionStore.Set(action);
            RememberAdaptation(
                userId,
                session.Id,
                proposal.ProposalId,
                proposal.ExpiresAtUtc,
                proposal.Changes,
                [LiveAdaptationReason.Performance]);

            var omittedName = result.Changes.Single().ExerciseName;
            var proposalText = $"{weightLimitation}{evidenceText} Puedo mantener {result.CurrentExerciseName} y omitir {omittedName} para reducir el volumen posterior de hoy. ¿Querés aplicar este ajuste?";
            return CreateDirectResponse(clientTurnId, proposalText, ToDto(action));
        }

        private LiveCoachRespondResponseDto CreateCombinedAdaptationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            LiveAdaptationSignals signals)
        {
            if (signals.RemainingMinutes is < LiveTimeConstraintAdaptationEngine.MinimumRemainingMinutes or
                > LiveTimeConstraintAdaptationEngine.MaximumRemainingMinutes)
            {
                return CreateDirectResponse(
                    clientTurnId,
                    "Necesito un tiempo entre 5 y 120 minutos para ajustar la sesión.");
            }

            if (signals.RemainingMinutes.HasValue)
            {
                RememberTimeConstraint(
                    userId,
                    session.Id,
                    signals.RemainingMinutes.Value,
                    DateTime.UtcNow);
            }

            var result = LiveAdaptationOrchestrator.Evaluate(session, signals);
            if (result.Status == LiveCombinedAdaptationStatus.NoPendingWork)
                return CreateDirectResponse(clientTurnId, "Ya completaste todas las series de la sesión.");
            if (result.Status == LiveCombinedAdaptationStatus.NoApplicableChange)
            {
                var responseText = signals.RemainingMinutes.HasValue &&
                    result.EstimatedMinutesBefore <= signals.RemainingMinutes.Value
                        ? $"Lo pendiente ya entra en aproximadamente {result.EstimatedMinutesBefore} minutos y no hay un recorte adicional seguro para aplicar."
                        : "No hay un recorte adicional seguro que pueda aplicar sin tocar el ejercicio actual o trabajo ya iniciado.";
                return CreateDirectResponse(clientTurnId, responseText);
            }

            var nowUtc = DateTime.UtcNow;
            var expiresAtUtc = nowUtc.AddSeconds(45);
            var actionId = Guid.NewGuid();
            var proposal = new LiveCombinedAdaptationProposal(
                actionId,
                userId,
                session.Id,
                nowUtc,
                expiresAtUtc,
                signals,
                result.AppliedReasons,
                result.PerformanceEvidence,
                result.EstimatedMinutesBefore,
                result.EstimatedMinutesAfter,
                LiveTimeConstraintFingerprint.Create(session),
                result.Changes,
                BuildCombinedPublicSummary(signals, result.AppliedReasons));
            var current = CurrentPendingTrainingSetResolver.Resolve(session)!;
            var action = new PendingLiveCoachAction(
                actionId,
                userId,
                session.Id,
                current.ExerciseSession.Id,
                current.SetNumber,
                0,
                0,
                null,
                expiresAtUtc,
                LiveCoachActionTypes.ApplyCombinedAdaptation,
                AdaptationProposal: proposal);
            _pendingActionStore.Set(action);
            RememberAdaptation(
                userId,
                session.Id,
                proposal.ProposalId,
                proposal.ExpiresAtUtc,
                proposal.Changes,
                proposal.Reasons);

            var reasons = FormatCombinedReasons(signals, result);
            var omittedNames = FormatNaturalList(result.Changes.Select(change => change.ExerciseName));
            var timeResult = signals.RemainingMinutes.HasValue
                ? $" Lo pendiente quedaría en ~{result.EstimatedMinutesAfter} minutos."
                : string.Empty;
            var text = $"{reasons}, puedo mantener {result.CurrentExerciseName} y omitir {omittedNames}.{timeResult} ¿Querés aplicar este ajuste?";
            return CreateDirectResponse(clientTurnId, text, ToDto(action));
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

        private LiveCoachRespondResponseDto ResolveSessionFollowUp(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            string clientTurnId,
            LiveCoachSessionFollowUp followUp,
            LiveCoachSessionConversationContext conversation,
            PendingLiveCoachActionLookupResult pendingLookup,
            DateTime nowUtc)
        {
            if (followUp.Kind == LiveCoachSessionFollowUpKind.BestSetCriterion &&
                followUp.BestSetCriterion.HasValue)
            {
                var exercise = FindReferencedExercise(session, conversation);
                if (exercise is null)
                    return ClearInvalidExerciseReference(userId, session.Id, clientTurnId, nowUtc);

                RememberConversation(
                    userId,
                    session.Id,
                    current => current with
                    {
                        LastIntentKind = LiveCoachIntentKind.BestSetToday,
                        LastReferencedExerciseSessionId = exercise.Id,
                        LastBestSetCriterion = followUp.BestSetCriterion.Value,
                        LastPerformanceQueryKind = LiveCoachIntentKind.BestSetToday
                    },
                    nowUtc);
                return CreateDirectResponse(
                    clientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveBestSetToday(
                        exercise,
                        followUp.BestSetCriterion.Value));
            }

            if (followUp.Kind == LiveCoachSessionFollowUpKind.RemainingExercises)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.RemainingExercises,
                    nowUtc);
                return CreateDirectResponse(
                    clientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveRemainingExercises(session));
            }

            if (followUp.Kind == LiveCoachSessionFollowUpKind.SessionProgress)
            {
                RememberAuthoritativeIntent(
                    session,
                    userId,
                    LiveCoachIntentKind.SessionProgress,
                    nowUtc);
                return CreateDirectResponse(
                    clientTurnId,
                    LiveCoachPerformanceQueryResolver.ResolveSessionProgress(session));
            }

            if (followUp.Kind == LiveCoachSessionFollowUpKind.NavigateToReferencedExercise)
            {
                var exercise = FindReferencedExercise(session, conversation);
                if (exercise is null)
                    return ClearInvalidExerciseReference(userId, session.Id, clientTurnId, nowUtc);

                RememberConversation(
                    userId,
                    session.Id,
                    current => current with
                    {
                        LastIntentKind = LiveCoachIntentKind.NavigateToExercise,
                        LastReferencedExerciseSessionId = exercise.Id
                    },
                    nowUtc);
                return CreateDirectResponse(
                    clientTurnId,
                    $"Mostrando {exercise.Ejercicio.Nombre}.",
                    clientAction: new LiveCoachClientActionDto(
                        "NavigateToExercise",
                        ExerciseSessionId: exercise.Id));
            }

            if (followUp.Kind == LiveCoachSessionFollowUpKind.AdaptationChanges)
            {
                var adaptation = conversation.LastAdaptation;
                if (adaptation is null)
                    return CreateDirectResponse(clientTurnId, "No tengo un ajuste reciente para revisar.");

                var affected = adaptation.AffectedExerciseSessionIds
                    .Select(id => session.Ejercicios.FirstOrDefault(exercise => exercise.Id == id))
                    .ToArray();
                if (affected.Any(exercise => exercise is null))
                {
                    RememberConversation(
                        userId,
                        session.Id,
                        current => current with { LastAdaptation = null },
                        nowUtc);
                    return CreateDirectResponse(
                        clientTurnId,
                        "La sesión cambió. Pedime de nuevo el ajuste si todavía lo necesitás.");
                }

                var exercises = affected.Select(exercise => exercise!).ToArray();
                var wasApplied = exercises.All(exercise => exercise.Omitido);
                var isPending = pendingLookup.Status == PendingLiveCoachActionLookupStatus.Active &&
                    pendingLookup.Action?.ActionId == adaptation.ProposalId &&
                    IsAdaptationAction(pendingLookup.Action.Type) &&
                    nowUtc <= adaptation.ProposalExpiresAtUtc;
                if (!wasApplied && !isPending)
                {
                    RememberConversation(
                        userId,
                        session.Id,
                        current => current with { LastAdaptation = null },
                        nowUtc);
                    return CreateDirectResponse(
                        clientTurnId,
                        "No hay un ajuste vigente en esta sesión.");
                }

                var names = FormatNaturalList(exercises.Select(exercise => exercise.Ejercicio.Nombre));
                RememberConversation(
                    userId,
                    session.Id,
                    current => current with { LastIntentKind = LiveCoachIntentKind.LiveAdaptationCombinedSignals },
                    nowUtc);
                return CreateDirectResponse(
                    clientTurnId,
                    wasApplied
                        ? $"Omití {names} en esta sesión."
                        : $"El ajuste propone omitir {names}.",
                    pendingAction: isPending && pendingLookup.Action is not null
                        ? ToDto(pendingLookup.Action)
                        : null);
            }

            if (followUp.Kind == LiveCoachSessionFollowUpKind.ReferencedExerciseCompletion)
            {
                var exercise = FindReferencedExercise(session, conversation);
                if (exercise is null)
                    return ClearInvalidExerciseReference(userId, session.Id, clientTurnId, nowUtc);

                var completedSets = exercise.Series
                    .Where(set => set.EstaRealmenteCompletada())
                    .Select(set => set.NumeroSerie)
                    .Distinct()
                    .Count(number => number >= 1 && number <= exercise.SeriesObjetivo);
                var text = exercise.Omitido
                    ? $"{exercise.Ejercicio.Nombre} está omitido; registraste {completedSets} de {exercise.SeriesObjetivo} series."
                    : completedSets >= exercise.SeriesObjetivo
                        ? $"Sí. Ya completaste {exercise.Ejercicio.Nombre}."
                        : $"Todavía no. Llevás {completedSets} de {exercise.SeriesObjetivo} series en {exercise.Ejercicio.Nombre}.";
                return CreateDirectResponse(clientTurnId, text);
            }

            if (followUp.Kind == LiveCoachSessionFollowUpKind.ProactiveEvidence &&
                conversation.LastProactiveTrigger is not null)
            {
                var proactive = conversation.LastProactiveTrigger;
                if (proactive.ExerciseSessionId.HasValue &&
                    !session.Ejercicios.Any(exercise =>
                        exercise.Id == proactive.ExerciseSessionId.Value))
                {
                    RememberConversation(
                        userId,
                        session.Id,
                        current => current with { LastProactiveTrigger = null },
                        nowUtc);
                    return CreateDirectResponse(
                        clientTurnId,
                        "La sesión cambió y esa sugerencia ya no está vigente.");
                }
                return CreateDirectResponse(
                    clientTurnId,
                    proactive.EvidenceText);
            }

            return CreateDirectResponse(
                clientTurnId,
                "¿Querés comparar con la serie anterior o con la sesión anterior?");
        }

        private static Entrenate.Domain.Entidades.EjercicioSesion? FindReferencedExercise(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            LiveCoachSessionConversationContext conversation)
            => conversation.LastReferencedExerciseSessionId.HasValue
                ? session.Ejercicios.FirstOrDefault(
                    exercise => exercise.Id == conversation.LastReferencedExerciseSessionId.Value)
                : null;

        private LiveCoachRespondResponseDto ClearInvalidExerciseReference(
            string userId,
            Guid sessionId,
            string clientTurnId,
            DateTime nowUtc)
        {
            RememberConversation(
                userId,
                sessionId,
                current => current with
                {
                    LastReferencedExerciseSessionId = null,
                    LastBestSetCriterion = null
                },
                nowUtc);
            return CreateDirectResponse(clientTurnId, "¿A qué ejercicio te referís?");
        }

        private LiveCoachRespondResponseDto CreateBestSetClarificationResponse(
            string clientTurnId)
            => CreateDirectResponse(
                clientTurnId,
                LiveCoachPerformanceQueryResolver.ResolveBestSetToday(),
                clarification: new LiveCoachClarificationDto(
                    "BestSetCriterion",
                    [
                        new("peso", "Peso"),
                        new("repeticiones", "Repeticiones"),
                        new("volumen", "Volumen")
                    ]));

        private void RememberAuthoritativeIntent(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            LiveCoachIntentKind intent,
            DateTime nowUtc,
            bool performanceQuery = false)
        {
            var authoritative = CurrentPendingTrainingSetResolver.Resolve(session)?.ExerciseSession;
            RememberConversation(
                userId,
                session.Id,
                current => current with
                {
                    LastIntentKind = intent,
                    LastReferencedExerciseSessionId = authoritative?.Id ?? current.LastReferencedExerciseSessionId,
                    LastAuthoritativeExerciseSessionId = authoritative?.Id,
                    LastPerformanceQueryKind = performanceQuery
                        ? intent
                        : current.LastPerformanceQueryKind
                },
                nowUtc);
        }

        private void RememberNavigationResponse(
            Entrenate.Domain.Entidades.SesionEntrenamiento session,
            string userId,
            LiveCoachIntentKind intent,
            LiveCoachRespondResponseDto response,
            DateTime nowUtc)
        {
            var exerciseId = response.ClientAction?.ExerciseSessionId;
            if (!exerciseId.HasValue ||
                !session.Ejercicios.Any(exercise => exercise.Id == exerciseId.Value))
            {
                return;
            }

            var authoritativeId = CurrentPendingTrainingSetResolver.Resolve(session)?.ExerciseSession.Id;
            RememberConversation(
                userId,
                session.Id,
                current => current with
                {
                    LastIntentKind = intent,
                    LastReferencedExerciseSessionId = exerciseId.Value,
                    LastAuthoritativeExerciseSessionId = authoritativeId
                },
                nowUtc);
        }

        private void RememberAdaptation(
            string userId,
            Guid sessionId,
            Guid proposalId,
            DateTime expiresAtUtc,
            IReadOnlyCollection<LiveAdaptationChange> changes,
            IReadOnlyCollection<LiveAdaptationReason> reasons)
        {
            RememberConversation(
                userId,
                sessionId,
                current => current with
                {
                    LastIntentKind = reasons.Count > 1
                        ? LiveCoachIntentKind.LiveAdaptationCombinedSignals
                        : reasons.Single() switch
                        {
                            LiveAdaptationReason.TimeConstraint => LiveCoachIntentKind.LiveAdaptationTimeConstraint,
                            LiveAdaptationReason.Fatigue => LiveCoachIntentKind.LiveAdaptationFatigueSignal,
                            LiveAdaptationReason.Performance => LiveCoachIntentKind.LiveAdaptationPerformanceSignal,
                            _ => LiveCoachIntentKind.LiveAdaptationCombinedSignals
                        },
                    LastAdaptation = new LiveCoachAdaptationReference(
                        proposalId,
                        changes.Select(change => change.ExerciseSessionId).Distinct().ToArray(),
                        reasons.Distinct().ToArray(),
                        expiresAtUtc)
                },
                DateTime.UtcNow);
        }

        private void RememberTimeConstraint(
            string userId,
            Guid sessionId,
            int availableMinutes,
            DateTime nowUtc)
        {
            RememberConversation(
                userId,
                sessionId,
                current => current with
                {
                    LastTimeConstraint = new LiveCoachTimeConstraintReference(
                        availableMinutes,
                        nowUtc)
                },
                nowUtc);
        }

        private void RememberConversation(
            string userId,
            Guid sessionId,
            Func<LiveCoachSessionConversationContext, LiveCoachSessionConversationContext> update,
            DateTime nowUtc)
        {
            var current = _sessionConversationStore.Get(userId, sessionId, nowUtc) ??
                new LiveCoachSessionConversationContext(userId, sessionId);
            _sessionConversationStore.Set(update(current), nowUtc);
        }

        private LiveCoachRespondResponseDto CreateDirectResponse(
            string clientTurnId,
            string text,
            PendingLiveCoachActionDto? pendingAction = null,
            Entrenate.Application.TrainingSessions.DTOs.TrainingSessionDto? updatedSession = null,
            LiveCoachClientActionDto? clientAction = null,
            LiveCoachClarificationDto? clarification = null)
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
                clientAction,
                clarification);
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

            if (IsAdaptationAction(action.Type) && action.AdaptationProposal is not null)
                return action.AdaptationProposal.PublicSummary;

            var weight = action.WeightKg.ToString("0.##", CultureInfo.InvariantCulture);
            return $"{weight} kg × {action.Repetitions}" +
                (action.Rir.HasValue ? $" · RIR {action.Rir.Value}" : string.Empty);
        }

        private static string FormatNaturalList(IEnumerable<string> values)
        {
            var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            return items.Length switch
            {
                0 => "lo que viene después",
                1 => items[0],
                _ => $"{string.Join(", ", items[..^1])} y {items[^1]}"
            };
        }

        private static string FormatPerformanceEvidence(LivePerformanceEvidence evidence)
        {
            var repetitionTarget = evidence.TargetMinimumRepetitions == evidence.TargetMaximumRepetitions
                ? evidence.TargetMinimumRepetitions.ToString(CultureInfo.InvariantCulture)
                : $"{evidence.TargetMinimumRepetitions}–{evidence.TargetMaximumRepetitions}";
            if (evidence.MissedPrescription)
            {
                return $"En la última serie hiciste {evidence.ActualRepetitions} reps y el objetivo era {repetitionTarget}, así que quedaste por debajo de la prescripción.";
            }

            if (evidence.ExceededTargetEffort && evidence.ActualRir.HasValue)
            {
                return $"En la última serie registraste RIR {evidence.ActualRir.Value} y el objetivo era RIR {evidence.TargetMinimumRir}–{evidence.TargetMaximumRir}, así que el esfuerzo fue mayor al prescripto.";
            }

            return $"Tu última serie estuvo dentro de la prescripción: {evidence.ActualRepetitions} reps sobre un objetivo de {repetitionTarget}.";
        }

        private static string FormatCombinedReasons(
            LiveAdaptationSignals signals,
            LiveCombinedAdaptationResult result)
        {
            var reasons = new List<string>();
            if (signals.RemainingMinutes.HasValue)
                reasons.Add($"te quedan {signals.RemainingMinutes.Value} minutos");
            if (signals.FatigueSeverity.HasValue)
            {
                reasons.Add(signals.FatigueSeverity == LiveFatigueSeverity.High
                    ? "me dijiste que estás muy cansado"
                    : "me dijiste que estás cansado");
            }
            if (result.AppliedReasons.Contains(LiveAdaptationReason.Performance) &&
                result.PerformanceEvidence is not null)
            {
                reasons.Add("en la última serie quedaste por debajo de la prescripción");
            }

            return $"Como {FormatNaturalList(reasons)}";
        }

        private static string BuildCombinedPublicSummary(
            LiveAdaptationSignals signals,
            IReadOnlyCollection<LiveAdaptationReason> appliedReasons)
        {
            var labels = new List<string>();
            if (signals.RemainingMinutes.HasValue)
                labels.Add("tiempo disponible");
            if (signals.FatigueSeverity.HasValue)
                labels.Add("cansancio reportado");
            if (appliedReasons.Contains(LiveAdaptationReason.Performance))
                labels.Add("rendimiento registrado");
            return $"Ajustar sesión por {FormatNaturalList(labels)}";
        }

        private static string GetAdaptationUnavailableText(Exception exception)
            => exception.Message.Contains("expir", StringComparison.OrdinalIgnoreCase)
                ? "Ese ajuste ya venció. Pedímelo de nuevo y lo recalculo."
                : "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo.";

        private static bool IsAdaptationAction(string actionType)
            => actionType is LiveCoachActionTypes.ApplyTimeConstraintAdaptation or
                LiveCoachActionTypes.ApplyFatigueAdaptation or
                LiveCoachActionTypes.ApplyPerformanceAdaptation or
                LiveCoachActionTypes.ApplyCombinedAdaptation;

        private static bool IsAdaptationIntent(LiveCoachIntentKind intentKind)
            => intentKind is LiveCoachIntentKind.LiveAdaptationTimeConstraint or
                LiveCoachIntentKind.LiveAdaptationFatigueSignal or
                LiveCoachIntentKind.LiveAdaptationPerformanceSignal or
                LiveCoachIntentKind.LiveAdaptationCombinedSignals;

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
