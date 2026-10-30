using System;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Context;
using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Prompt;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Entrenate.Application.Coach.LiveCoach.Tools;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Enums;
using Entrenate.Application.TrainingSessions;
using System.Globalization;
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

            // 2. Intent routing
            var routing = _intentRouter.Route(request.Text);

            if (routing.Kind == LiveCoachIntentKind.RegisterCurrentSet)
            {
                return CreateRegisterCurrentSetResponse(
                    session,
                    userId,
                    request.ClientTurnId,
                    request.Text);
            }

            if (!string.IsNullOrWhiteSpace(routing.DirectAnswer))
            {
                var directId = Guid.NewGuid();
                _responseCache.Set(directId, routing.DirectAnswer, TimeSpan.FromSeconds(60));

                return new LiveCoachRespondResponseDto(
                    directId,
                    request.ClientTurnId,
                    routing.DirectAnswer,
                    false,
                    true);
            }

            // 3. Authorized context projection
            var nowUtc = DateTime.UtcNow;
            var context = _contextBuilder.Build(session, request.ClientSnapshot, nowUtc);

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

        private LiveCoachRespondResponseDto CreateDirectResponse(string clientTurnId, string text)
        {
            var responseId = Guid.NewGuid();
            _responseCache.Set(responseId, text, TimeSpan.FromSeconds(60));
            return new LiveCoachRespondResponseDto(
                responseId,
                clientTurnId,
                text,
                false,
                true);
        }
    }
}
