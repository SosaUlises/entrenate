namespace Entrenate.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerarToken(
            string usuarioId,
            string email);
    }
}
