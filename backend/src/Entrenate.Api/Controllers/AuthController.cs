using Entrenate.Application.Auth.Commands.ChangePassword;
using Entrenate.Application.Auth.Commands.Login;
using Entrenate.Application.Auth.Commands.Register;
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
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordCommand command,
            CancellationToken cancellationToken)
        {
            await _sender.Send(command, cancellationToken);

            return NoContent();
        }
    }
}
