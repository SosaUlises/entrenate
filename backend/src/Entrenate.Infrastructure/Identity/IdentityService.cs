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

        public async Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new UnauthorizedAccessException(
                    "El usuario autenticado no existe.");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);

            if (!result.Succeeded)
            {
                var invalidPassword = result.Errors.Any(error =>
                    error.Code == nameof(
                        IdentityErrorDescriber.PasswordMismatch));

                if (invalidPassword)
                {
                    throw new UnauthorizedAccessException(
                        "La contraseña actual es incorrecta.");
                }

                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"No se pudo cambiar la contraseña. {errors}");
            }
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                return null;
            }

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task ResetPasswordAsync(
            string email,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                throw new UnauthorizedAccessException(
                    "El token de recuperación no es válido.");
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                token,
                newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"No se pudo restablecer la contraseña. {errors}");
            }
        }
    }
}
