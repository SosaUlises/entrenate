using Entrenate.Application.Auth.DTOs;
using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Auth.Commands.Login
{
    public class LoginCommandHandler
     : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IIdentityService _identityService;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            IIdentityService identityService,
            ITokenService tokenService)
        {
            _identityService = identityService;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var userId = await _identityService.ValidateCredentialsAsync(
                request.Email,
                request.Password,
                cancellationToken);

            if (userId is null)
            {
                throw new UnauthorizedAccessException(
                    "Email o contraseña incorrectos.");
            }

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
