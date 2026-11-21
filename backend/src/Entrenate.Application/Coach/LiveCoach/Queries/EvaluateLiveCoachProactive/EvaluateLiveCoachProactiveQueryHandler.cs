using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.Clarifications;
using Entrenate.Application.Coach.LiveCoach.Conversation;
using Entrenate.Application.Coach.LiveCoach.DTOs;
using Entrenate.Application.Coach.LiveCoach.Proactive;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Enums;
using MediatR;

namespace Entrenate.Application.Coach.LiveCoach.Queries.EvaluateLiveCoachProactive
{
    public sealed class EvaluateLiveCoachProactiveQueryHandler
        : IRequestHandler<EvaluateLiveCoachProactiveQuery, LiveCoachProactiveEvaluationDto>
    {
        private const int MaximumRememberedTriggers = 8;
        private readonly ITrainingSessionRepository _sessionRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILiveCoachPendingActionStore _pendingActionStore;
        private readonly ILiveCoachClarificationStore _clarificationStore;
        private readonly ILiveCoachSessionConversationStore _conversationStore;

        public EvaluateLiveCoachProactiveQueryHandler(
            ITrainingSessionRepository sessionRepository,
            ICurrentUserService currentUserService,
            ILiveCoachPendingActionStore pendingActionStore,
            ILiveCoachClarificationStore clarificationStore,
            ILiveCoachSessionConversationStore conversationStore)
        {
            _sessionRepository = sessionRepository;
            _currentUserService = currentUserService;
            _pendingActionStore = pendingActionStore;
            _clarificationStore = clarificationStore;
            _conversationStore = conversationStore;
        }

        public async Task<LiveCoachProactiveEvaluationDto> Handle(
            EvaluateLiveCoachProactiveQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            var session = await _sessionRepository.GetByIdAndUserIdAsNoTrackingAsync(
                request.SessionId,
                userId,
                cancellationToken);
            if (session is null)
                throw new NotFoundException("Sesión de entrenamiento no encontrada.");
            if (session.Estado != EstadoSesionEntrenamiento.EnCurso)
            {
                _conversationStore.Clear(userId, session.Id);
                return new(null);
            }

            var nowUtc = DateTime.UtcNow;
            var interactionBlocked =
                _pendingActionStore.FindActive(userId, session.Id, nowUtc).Status ==
                    PendingLiveCoachActionLookupStatus.Active ||
                _clarificationStore.FindActive(userId, session.Id, nowUtc).Status ==
                    PendingLiveCoachClarificationLookupStatus.Active;

            var context = _conversationStore.Get(userId, session.Id, nowUtc) ??
                new LiveCoachSessionConversationContext(userId, session.Id);
            var candidates = LiveCoachProactiveSuggestionEngine.Evaluate(
                session,
                request.Snapshot,
                context.LastTimeConstraint,
                nowUtc,
                interactionBlocked);
            var candidate = candidates.FirstOrDefault();
            if (candidate is null ||
                !LiveCoachProactiveSuggestionEngine.CanEmit(
                    candidate,
                    context.RecentProactiveTriggers,
                    nowUtc))
            {
                return new(null);
            }

            var reference = new LiveCoachProactiveTriggerReference(
                candidate.Type,
                candidate.ExerciseSessionId,
                candidate.SourceFingerprint,
                nowUtc,
                candidate.EvidenceText);
            var recent = (context.RecentProactiveTriggers ?? [])
                .Where(item => item.Type != candidate.Type ||
                    item.ExerciseSessionId != candidate.ExerciseSessionId)
                .Append(reference)
                .OrderByDescending(item => item.TriggeredAtUtc)
                .Take(MaximumRememberedTriggers)
                .ToArray();
            _conversationStore.Set(
                context with
                {
                    LastReferencedExerciseSessionId = candidate.ExerciseSessionId ??
                        context.LastReferencedExerciseSessionId,
                    RecentProactiveTriggers = recent,
                    LastProactiveTrigger = reference
                },
                nowUtc);

            return new(new LiveCoachProactiveSuggestionDto(
                candidate.Type.ToString(),
                candidate.Text,
                candidate.ActionLabel,
                candidate.ActionPrompt));
        }
    }
}
