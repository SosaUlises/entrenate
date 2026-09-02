using Entrenate.Application.Auth.DTOs;
using MediatR;

namespace Entrenate.Application.Auth.Commands.Login
{
    public record LoginCommand(
     string Email,
     string Password
 ) : IRequest<AuthResponse>;
}
