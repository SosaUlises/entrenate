using Entrenate.Application.Auth.DTOs;
using MediatR;

namespace Entrenate.Application.Auth.Commands.Register
{
    public record RegisterCommand(
    string Nombre,
    string Email,
    string Password
) : IRequest<AuthResponse>;
}
