using Entrenate.Application.Auth.DTOs;
using MediatR;

namespace Entrenate.Application.Auth.Commands.Register
{
    public record RegisterCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;
}
