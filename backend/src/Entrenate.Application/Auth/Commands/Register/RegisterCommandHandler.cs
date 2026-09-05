using Entrenate.Application.Auth.DTOs;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Auth.Commands.Register
{
    public class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;

        public RegisterCommandHandler(
            IIdentityService identityService,
            ITokenService tokenService)
        {
            _identityService = identityService;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var userId = await _identityService.CreateUserAsync(
                 request.Nombre,
                 request.Email,
                 request.Password,
                 cancellationToken);

            var token = _tokenService.GenerateToken(
                userId,
                request.Email);

            return new AuthResponse(
                userId,
                request.Email,
                token);
        }
    }
}
