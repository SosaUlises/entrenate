using MediatR;

namespace Entrenate.Application.Auth.Commands.ForgotPassword
{
    public record ForgotPasswordCommand(
     string Email
 ) : IRequest;
}
