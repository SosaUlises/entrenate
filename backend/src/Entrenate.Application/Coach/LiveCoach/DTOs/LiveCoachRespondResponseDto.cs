using System;

namespace Entrenate.Application.Coach.LiveCoach.DTOs
{
    public sealed record LiveCoachRespondResponseDto(
        Guid ResponseId,
        string ClientTurnId,
        string Text,
        string IntentKind,
        bool IsStaleContext,
        bool CanSynthesizeVoice);
}

