using MediatR;

namespace Entrenate.Application.Auth.Commands.ResetPassword
{
    public record ResetPasswordCommand(
       string Email,
       string Token,
       string NewPassword
   ) : IRequest;
}
