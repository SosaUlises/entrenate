namespace Entrenate.Application.Auth.DTOs
{
    public record CurrentUserDto(
        string UserId,
        string Nombre,
        string Email
    );
}
