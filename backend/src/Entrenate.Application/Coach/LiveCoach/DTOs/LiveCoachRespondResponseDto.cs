using System;
using Entrenate.Application.TrainingSessions.DTOs;

namespace Entrenate.Application.Coach.LiveCoach.DTOs
{
    public sealed record LiveCoachRespondResponseDto(
        Guid ResponseId,
        string ClientTurnId,
        string Text,
        bool IsStaleContext,
        bool CanSynthesizeVoice,
        PendingLiveCoachActionDto? PendingAction = null,
        TrainingSessionDto? UpdatedSession = null,
        LiveCoachClientActionDto? ClientAction = null,
        LiveCoachClarificationDto? Clarification = null);

    public sealed record LiveCoachClarificationDto(
        string Kind,
        IReadOnlyCollection<LiveCoachClarificationOptionDto> Options);

    public sealed record LiveCoachClarificationOptionDto(
        string Value,
        string Label);

    public sealed record LiveCoachClientActionDto(
        string Type,
        int? DurationSeconds = null,
        Guid? ExerciseSessionId = null);

    public sealed record LiveCoachActionConfirmationDto(
        TrainingSessionDto UpdatedSession,
        string Text,
        LiveCoachClientActionDto? ClientAction = null);

    public sealed record PendingLiveCoachActionDto(
        Guid ActionId,
        string Type,
        string Summary,
        DateTime ExpiresAtUtc);
}

