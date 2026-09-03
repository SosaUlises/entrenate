namespace Entrenate.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(
            string userId,
            string email);
    }
}
