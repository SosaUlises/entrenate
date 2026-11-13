using System;

namespace Entrenate.Application.Coach.LiveCoach.DTOs
{
    public sealed record LiveCoachTranscriptionResponseDto(
        Guid InteractionId,
        string ClientTurnId,
        string Transcript,
        decimal? Confidence,
        string? DetectedLocale,
        bool IsFinal,
        string Status);
}

