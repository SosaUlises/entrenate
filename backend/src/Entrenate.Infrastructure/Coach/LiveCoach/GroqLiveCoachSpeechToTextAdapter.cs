using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Infrastructure.Coach.Conversation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class GroqLiveCoachSpeechToTextAdapter : ILiveCoachSpeechToText
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly GroqSettings _settings;
        private readonly ILogger<GroqLiveCoachSpeechToTextAdapter> _logger;

        public GroqLiveCoachSpeechToTextAdapter(
            HttpClient httpClient,
            IOptions<GroqSettings> options,
            ILogger<GroqLiveCoachSpeechToTextAdapter> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<LiveCoachTranscriptResult> TranscribeAsync(
            LiveCoachAudioInput input,
            CancellationToken cancellationToken = default)
        {
            var timeoutSeconds = _settings.SttTimeoutSeconds > 0 ? _settings.SttTimeoutSeconds : 10;
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                using var form = new MultipartFormDataContent();

                var streamContent = new StreamContent(input.Content);
                var cleanMediaType = ExtractCleanMediaType(input.MediaType);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(cleanMediaType);

                var filename = ResolveFilename(input.MediaType);
                form.Add(streamContent, "file", filename);

                var modelName = string.IsNullOrWhiteSpace(_settings.SttModel)
                    ? "whisper-large-v3-turbo"
                    : _settings.SttModel;
                form.Add(new StringContent(modelName), "model");
                form.Add(new StringContent(string.IsNullOrWhiteSpace(input.Locale) ? "es" : input.Locale), "language");
                form.Add(new StringContent("verbose_json"), "response_format");

                _logger.LogInformation(
                    "Sending STT request to Groq. InteractionId: {InteractionId}, Bytes: {Bytes}, Media: {Media}, Model: {Model}",
                    input.InteractionId,
                    input.ByteLength,
                    cleanMediaType,
                    modelName);

                using var response = await _httpClient.PostAsync(
                    "audio/transcriptions",
                    form,
                    linkedCts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Groq STT returned HTTP {StatusCode}. InteractionId: {InteractionId}",
                        response.StatusCode,
                        input.InteractionId);

                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        return new LiveCoachTranscriptResult(
                            string.Empty,
                            null,
                            null,
                            true,
                            LiveCoachTranscriptStatus.UnsupportedAudio);
                    }

                    return new LiveCoachTranscriptResult(
                        string.Empty,
                        null,
                        null,
                        true,
                        LiveCoachTranscriptStatus.ProviderUnavailable);
                }

                var json = await response.Content.ReadAsStringAsync(linkedCts.Token);
                var payload = JsonSerializer.Deserialize<GroqTranscriptionEnvelope>(json, JsonOptions);

                if (payload == null || string.IsNullOrWhiteSpace(payload.Text))
                {
                    return new LiveCoachTranscriptResult(
                        string.Empty,
                        null,
                        payload?.Language,
                        true,
                        LiveCoachTranscriptStatus.EmptyTranscript);
                }

                return new LiveCoachTranscriptResult(
                    payload.Text.Trim(),
                    null,
                    payload.Language ?? "es",
                    true,
                    LiveCoachTranscriptStatus.Success);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Groq STT timed out after {Timeout} seconds. InteractionId: {InteractionId}",
                    timeoutSeconds,
                    input.InteractionId);

                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.Timeout);
            }
            catch (OperationCanceledException)
            {
                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.Canceled);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Groq STT connection failed. InteractionId: {InteractionId}",
                    input.InteractionId);

                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.ProviderUnavailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during Groq STT processing. InteractionId: {InteractionId}",
                    input.InteractionId);

                return new LiveCoachTranscriptResult(
                    string.Empty,
                    null,
                    null,
                    true,
                    LiveCoachTranscriptStatus.ProviderUnavailable);
            }
        }

        private static string ExtractCleanMediaType(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
                return "audio/webm";

            var parts = mediaType.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length > 0 ? parts[0] : "audio/webm";
        }

        private static string ResolveFilename(string mediaType)
        {
            var clean = mediaType?.ToLowerInvariant() ?? string.Empty;
            if (clean.Contains("mp4") || clean.Contains("aac") || clean.Contains("m4a"))
                return "audio.mp4";
            if (clean.Contains("ogg"))
                return "audio.ogg";
            if (clean.Contains("wav"))
                return "audio.wav";
            if (clean.Contains("mpeg") || clean.Contains("mp3"))
                return "audio.mp3";

            return "audio.webm";
        }

        private sealed record GroqTranscriptionEnvelope(
            string? Text,
            string? Language,
            double? Duration);
    }
}

