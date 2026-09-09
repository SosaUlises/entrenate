namespace Entrenate.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<string> CreateUserAsync(
            string nombre,
            string email,
            string password,
            CancellationToken cancellationToken = default);

        Task<string?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default);

        Task<global::Entrenate.Application.Auth.DTOs.CurrentUserDto?> GetUserByIdAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task ChangePasswordAsync(
            string userId,
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default);

        Task<string?> GeneratePasswordResetTokenAsync(
            string email,
            CancellationToken cancellationToken = default);

        Task ResetPasswordAsync(
            string email,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default);
    }
}
