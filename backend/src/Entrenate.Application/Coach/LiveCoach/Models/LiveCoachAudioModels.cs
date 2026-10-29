using System;
using System.IO;

namespace Entrenate.Application.Coach.LiveCoach.Models
{
    public enum LiveCoachTranscriptStatus
    {
        Success,
        EmptyAudio,
        EmptyTranscript,
        LowConfidence,
        UnsupportedAudio,
        AudioTooLarge,
        AudioTooLong,
        Timeout,
        ProviderUnavailable,
        Canceled
    }

    public sealed record LiveCoachAudioInput(
        Stream Content,
        string MediaType,
        long ByteLength,
        int? DurationMilliseconds = null,
        string Locale = "es",
        Guid? InteractionId = null);

    public sealed record LiveCoachTranscriptResult(
        string Transcript,
        decimal? Confidence,
        string? DetectedLocale,
        bool IsFinal,
        LiveCoachTranscriptStatus Status);
}
