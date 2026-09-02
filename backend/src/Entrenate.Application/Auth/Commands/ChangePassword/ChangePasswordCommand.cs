using MediatR;

namespace Entrenate.Application.Auth.Commands.ChangePassword
{
    public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest;
}
