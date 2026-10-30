using System;

namespace Entrenate.Application.Coach.LiveCoach.DTOs
{
    public sealed record LiveCoachRespondResponseDto(
        Guid ResponseId,
        string ClientTurnId,
        string Text,
        bool IsStaleContext,
        bool CanSynthesizeVoice,
        PendingLiveCoachActionDto? PendingAction = null);

    public sealed record PendingLiveCoachActionDto(
        Guid ActionId,
        string Type,
        string Summary,
        DateTime ExpiresAtUtc);
}

