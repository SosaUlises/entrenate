namespace Entrenate.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<string> CreateUserAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default);

        Task<string?> ValidateCredentialsAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default);
    }
}
