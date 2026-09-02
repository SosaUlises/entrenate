using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Entrenate.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<string> CreateUserAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email
            };

            var result = await _userManager.CreateAsync(
                user,
                password);

            if (!result.Succeeded)
            {
                if (HasDuplicateUserError(result))
                {
                    throw new ConflictException(
                        "Ya existe un usuario registrado con ese email.");
                }

                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"No se pudo crear el usuario. {errors}");
            }

            return user.Id;
        }

        public async Task<string?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                return null;
            }

            var passwordIsValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    password);

            if (!passwordIsValid)
            {
                return null;
            }

            return user.Id;
        }

        private static bool HasDuplicateUserError(
            IdentityResult result)
        {
            return result.Errors.Any(error =>
                error.Code == nameof(IdentityErrorDescriber.DuplicateEmail) ||
                error.Code == nameof(IdentityErrorDescriber.DuplicateUserName));
        }
    }
}
