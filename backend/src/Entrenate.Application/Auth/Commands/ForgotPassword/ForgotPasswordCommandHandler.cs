using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Common.Settings;
using MediatR;
using System.Net;
using Microsoft.Extensions.Options;

namespace Entrenate.Application.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;

        public ForgotPasswordCommandHandler(
            IIdentityService identityService,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendOptions)
        {
            _identityService = identityService;
            _emailService = emailService;
            _frontendSettings = frontendOptions.Value;
        }

        public async Task Handle(
            ForgotPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var token =
                await _identityService.GeneratePasswordResetTokenAsync(
                    request.Email,
                    cancellationToken);

            if (token is null)
            {
                return;
            }

            var encodedEmail =
                WebUtility.UrlEncode(request.Email);

            var encodedToken =
                WebUtility.UrlEncode(token);

            var resetLink =
                $"{_frontendSettings.BaseUrl.TrimEnd('/')}/reset-password" +
                $"?email={encodedEmail}&token={encodedToken}";

            const string subject =
                "Restablecer contraseña - Entrenate";

            var body = $"""
            <h2>Restablecer contraseña</h2>

            <p>
                Recibimos una solicitud para restablecer
                la contraseña de tu cuenta de Entrenate.
            </p>

            <p>
                <a href="{resetLink}">
                    Restablecer contraseña
                </a>
            </p>

            <p>
                Si no solicitaste este cambio,
                podés ignorar este email.
            </p>
            """;

            await _emailService.SendAsync(
                request.Email,
                subject,
                body,
                cancellationToken);
        }
    }
}
