using Entrenate.Application.Auth.DTOs;
using MediatR;

namespace Entrenate.Application.Auth.Queries.GetCurrentUser
{
    public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
}
