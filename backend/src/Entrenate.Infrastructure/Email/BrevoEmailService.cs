using Entrenate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Entrenate.Infrastructure.Email
{
    public class BrevoEmailService : IEmailService
    {
        private const string SendEmailUrl =
            "https://api.brevo.com/v3/smtp/email";

        private readonly HttpClient _httpClient;
        private readonly EmailSettings _settings;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(
            HttpClient httpClient,
            IOptions<EmailSettings> options,
            ILogger<BrevoEmailService> logger)
        {
            _httpClient = httpClient;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException(
                    "El email destinatario es obligatorio.",
                    nameof(toEmail));
            }

            var payload = new
            {
                sender = new
                {
                    name = _settings.FromName,
                    email = _settings.FromEmail
                },
                to = new[]
                {
                new
                {
                    email = toEmail
                }
            },
                subject,
                htmlContent = htmlBody
            };

            var json = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                SendEmailUrl);

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            request.Headers.Add(
                "api-key",
                _settings.ApiKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email enviado correctamente a {ToEmail}.",
                    toEmail);

                return;
            }

            var responseBody =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            _logger.LogError(
                "Error enviando email con Brevo a {ToEmail}. " +
                "Status: {StatusCode}. Response: {ResponseBody}",
                toEmail,
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                "No se pudo enviar el email.");
        }
    }
}
