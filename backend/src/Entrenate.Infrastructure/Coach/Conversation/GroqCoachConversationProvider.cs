using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entrenate.Infrastructure.Coach.Conversation
{
    public class GroqCoachConversationProvider
        : ICoachConversationProvider
    {
        private const string ResponsesPath = "responses";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly GroqSettings _settings;
        private readonly ILogger<GroqCoachConversationProvider> _logger;

        public GroqCoachConversationProvider(
            HttpClient httpClient,
            IOptions<GroqSettings> options,
            ILogger<GroqCoachConversationProvider> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<CoachConversationResponse> GenerateResponseAsync(
            CoachConversationPrompt prompt,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prompt);

            if (string.IsNullOrWhiteSpace(prompt.Instructions) ||
                string.IsNullOrWhiteSpace(prompt.Input))
            {
                throw new ArgumentException(
                    "El prompt de conversación no puede estar vacío.",
                    nameof(prompt));
            }

            var payload = new GroqResponseRequest(
                _settings.Model,
                prompt.Instructions,
                prompt.Input,
                _settings.MaxOutputTokens);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                ResponsesPath)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload, JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.SendAsync(
                    request,
                    cancellationToken);
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "Groq agotó el tiempo de espera configurado.");

                throw new InvalidOperationException(
                    "El proveedor de conversación no respondió a tiempo.",
                    exception);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(
                    exception,
                    "No se pudo establecer comunicación con Groq.");

                throw new InvalidOperationException(
                    "No se pudo obtener una respuesta del proveedor de " +
                        "conversación.",
                    exception);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Groq respondió con status HTTP {StatusCode}.",
                        (int)response.StatusCode);

                    throw new InvalidOperationException(
                        "El proveedor de conversación rechazó la solicitud.");
                }

                try
                {
                    await using var responseStream = await response.Content
                        .ReadAsStreamAsync(cancellationToken);
                    var groqResponse = await JsonSerializer.DeserializeAsync<
                        GroqResponseEnvelope>(
                            responseStream,
                            JsonOptions,
                            cancellationToken);

                    var answer = ExtractAnswer(groqResponse);

                    return new CoachConversationResponse(answer);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(
                        exception,
                        "Groq devolvió una respuesta con formato inválido.");

                    throw new InvalidOperationException(
                        "El proveedor de conversación devolvió una " +
                            "respuesta inválida.",
                        exception);
                }
            }
        }

        private static string ExtractAnswer(
            GroqResponseEnvelope? response)
        {
            if (response is null ||
                !string.Equals(
                    response.Status,
                    "completed",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "El proveedor de conversación no completó la respuesta.");
            }

            var textSegments = (response.Output ?? [])
                .Where(item => string.Equals(
                    item.Type,
                    "message",
                    StringComparison.OrdinalIgnoreCase))
                .SelectMany(item => item.Content ?? [])
                .Where(content =>
                    string.Equals(
                        content.Type,
                        "output_text",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(content.Text))
                .Select(content => content.Text!.Trim())
                .ToList();

            if (textSegments.Count == 0)
            {
                throw new InvalidOperationException(
                    "El proveedor de conversación devolvió una respuesta " +
                        "sin texto.");
            }

            return string.Join(Environment.NewLine, textSegments);
        }

        private sealed record GroqResponseRequest(
            [property: JsonPropertyName("model")] string Model,
            [property: JsonPropertyName("instructions")] string Instructions,
            [property: JsonPropertyName("input")] string Input,
            [property: JsonPropertyName("max_output_tokens")]
                int MaxOutputTokens);

        private sealed record GroqResponseEnvelope(
            [property: JsonPropertyName("status")] string? Status,
            [property: JsonPropertyName("output")]
                IReadOnlyCollection<GroqOutputItem>? Output);

        private sealed record GroqOutputItem(
            [property: JsonPropertyName("type")] string? Type,
            [property: JsonPropertyName("content")]
                IReadOnlyCollection<GroqOutputContent>? Content);

        private sealed record GroqOutputContent(
            [property: JsonPropertyName("type")] string? Type,
            [property: JsonPropertyName("text")] string? Text);
    }
}
