using Entrenate.Application.Auth.Commands.ChangePassword;
using Entrenate.Application.Auth.Commands.ForgotPassword;
using Entrenate.Application.Auth.Commands.Login;
using Entrenate.Application.Auth.Commands.Register;
using Entrenate.Application.Auth.Commands.ResetPassword;
using Entrenate.Application.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;

        public AuthController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            RegisterCommand command,
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                command,
                cancellationToken);

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            LoginCommand command,
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                command,
                cancellationToken);

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new GetCurrentUserQuery(),
                cancellationToken);

            return Ok(response);
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordCommand command,
            CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordCommand command,
            CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);

            return Ok(new
            {
                message =
                    "Si existe una cuenta asociada al email, " +
                    "recibirás instrucciones para restablecer tu contraseña."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordCommand command,
            CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);

            return Ok(new
            {
                message = "La contraseña fue restablecida correctamente."
            });
        }
    }
}
