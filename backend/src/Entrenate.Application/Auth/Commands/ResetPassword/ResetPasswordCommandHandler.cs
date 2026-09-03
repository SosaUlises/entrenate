using Entrenate.Application.Common.Interfaces;
using MediatR;

namespace Entrenate.Application.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler
     : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IIdentityService _identityService;

        public ResetPasswordCommandHandler(
            IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            await _identityService.ResetPasswordAsync(
                request.Email,
                request.Token,
                request.NewPassword,
                cancellationToken);
        }
    }
}
