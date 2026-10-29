using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Entrenate.Application.Coach.LiveCoach.Models;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entrenate.Infrastructure.Coach.LiveCoach
{
    public sealed class ElevenLabsLiveCoachTextToSpeechAdapter : ILiveCoachTextToSpeech
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _httpClient;
        private readonly ElevenLabsSettings _settings;
        private readonly ILogger<ElevenLabsLiveCoachTextToSpeechAdapter> _logger;

        public ElevenLabsLiveCoachTextToSpeechAdapter(
            HttpClient httpClient,
            IOptions<ElevenLabsSettings> options,
            ILogger<ElevenLabsLiveCoachTextToSpeechAdapter> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<LiveCoachSpeechAudio> SynthesizeAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("El texto para síntesis de voz no puede estar vacío.", nameof(text));
            }

            var timeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 15;
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var voiceId = string.IsNullOrWhiteSpace(_settings.VoiceId)
                ? "21m00Tcm4TlvDq8ikWAM"
                : _settings.VoiceId.Trim();

            var outputFormat = string.IsNullOrWhiteSpace(_settings.OutputFormat)
                ? "mp3_44100_128"
                : _settings.OutputFormat.Trim();

            var url = $"text-to-speech/{voiceId}?output_format={outputFormat}";

            // Privacy: Payload only contains authorized text and voice parameters.
            // Zero user IDs, zero session IDs, zero training context.
            var payload = new
            {
                text,
                model_id = string.IsNullOrWhiteSpace(_settings.ModelId) ? "eleven_multilingual_v2" : _settings.ModelId.Trim(),
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75
                }
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                request.Headers.TryAddWithoutValidation("xi-api-key", _settings.ApiKey);
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    linkedCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogError("ElevenLabs TTS request timed out after {TimeoutSeconds}s.", timeoutSeconds);
                throw new ExternalServiceException("El servicio de voz agotó el tiempo de espera.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request to ElevenLabs TTS failed.");
                throw new ExternalServiceException("Error de conexión con el proveedor de voz.", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = string.Empty;
                    try
                    {
                        errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    }
                    catch
                    {
                        // Ignore read failure for error body
                    }

                    _logger.LogError(
                        "ElevenLabs TTS returned HTTP {StatusCode}. Details: {ErrorBody}",
                        response.StatusCode,
                        errorBody);

                    throw new ExternalServiceException(
                        $"El proveedor de voz respondió con error: {(int)response.StatusCode}.",
                        new HttpRequestException($"ElevenLabs TTS HTTP {(int)response.StatusCode}: {errorBody}"));
                }

                var mediaType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";

                var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                return new LiveCoachSpeechAudio(memoryStream, mediaType);
            }
        }
    }
}
