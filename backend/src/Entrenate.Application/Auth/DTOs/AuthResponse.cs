namespace Entrenate.Application.Auth.DTOs
{
    public record AuthResponse(
     string UserId,
     string Email,
     string Token
 );
}
