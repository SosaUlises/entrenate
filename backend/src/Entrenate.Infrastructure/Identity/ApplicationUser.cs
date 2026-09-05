using Microsoft.AspNetCore.Identity;

namespace Entrenate.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; } = string.Empty;
    }
}
