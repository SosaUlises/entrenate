using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Entrenate.Application.Coach.LiveCoach.Commands.TranscribeLiveAudio
{
    public sealed class TranscribeLiveAudioCommandHandler
        : IRequestHandler<TranscribeLiveAudioCommand, LiveCoachTranscriptResult>
    {
        public const long MinAudioBytes = 1024; // 1 KB
        public const long MaxAudioBytes = 2 * 1024 * 1024; // 2 MB
        public const int MaxDurationMs = 18000; // 18 seconds (15s client limit + margin)

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "audio/webm",
            "audio/ogg",
            "audio/mp4",
            "audio/aac",
            "audio/wav",
            "audio/wave",
            "audio/x-wav",
            "audio/mpeg",
            "audio/mp3",
            "audio/m4a",
            "audio/x-m4a"
        };

        private readonly ILiveCoachSpeechToText _speechToText;
        private readonly ILogger<TranscribeLiveAudioCommandHandler> _logger;

        public TranscribeLiveAudioCommandHandler(
            ILiveCoachSpeechToText speechToText,
            ILogger<TranscribeLiveAudioCommandHandler> logger)
        {
            _speechToText = speechToText;
            _logger = logger;
        }

        public async Task<LiveCoachTranscriptResult> Handle(
            TranscribeLiveAudioCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Validate payload size
            if (request.ByteLength < MinAudioBytes)
            {
                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.EmptyAudio);
            }

            if (request.ByteLength > MaxAudioBytes)
            {
                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.AudioTooLarge);
            }

            // 2. Validate duration
            if (request.DurationMilliseconds.HasValue && request.DurationMilliseconds.Value > MaxDurationMs)
            {
                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.AudioTooLong);
            }

            // 3. Validate media type
            var baseMimeType = ExtractBaseMimeType(request.MediaType);
            if (string.IsNullOrWhiteSpace(baseMimeType) || !AllowedMimeTypes.Contains(baseMimeType))
            {
                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.UnsupportedAudio);
            }

            // 4. Delegate to provider adapter
            var input = new LiveCoachAudioInput(
                request.AudioStream,
                request.MediaType,
                request.ByteLength,
                request.DurationMilliseconds,
                "es",
                request.InteractionId);

            var result = await _speechToText.TranscribeAsync(input, cancellationToken);

            // 5. Post-validation: empty or punctuation-only transcript
            if (result.Status == LiveCoachTranscriptStatus.Success)
            {
                if (IsEmptyOrPunctuationOnly(result.Transcript))
                {
                    return new LiveCoachTranscriptResult(
                        string.Empty,
                        result.Confidence,
                        result.DetectedLocale,
                        true,
                        LiveCoachTranscriptStatus.EmptyTranscript);
                }
            }

            return result;
        }

        private static string ExtractBaseMimeType(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
                return string.Empty;

            var parts = mediaType.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;
        }

        private static bool IsEmptyOrPunctuationOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            return text.All(c => char.IsWhiteSpace(c) || char.IsPunctuation(c));
        }
    }
}

