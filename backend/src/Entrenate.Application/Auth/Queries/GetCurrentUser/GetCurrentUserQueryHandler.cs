using Entrenate.Application.Auth.DTOs;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Auth.Queries.GetCurrentUser
{
    public class GetCurrentUserQueryHandler
        : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IIdentityService _identityService;

        public GetCurrentUserQueryHandler(
            ICurrentUserService currentUserService,
            IIdentityService identityService)
        {
            _currentUserService = currentUserService;
            _identityService = identityService;
        }

        public async Task<CurrentUserDto> Handle(
            GetCurrentUserQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var user = await _identityService.GetUserByIdAsync(
                userId,
                cancellationToken);

            if (user is null)
            {
                throw new NotFoundException(
                    "El usuario autenticado no existe.");
            }

            return user;
        }
    }
}
