namespace Entrenate.Domain.Entidades
{
    public class Ejercicio
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string GrupoMuscularPrincipal { get; set; } = string.Empty; // luego cambiarlo de string a GrupoMuscular class, verificar que recibimos de la API
        public string? Equipamiento { get; set; }
        public string? WorkoutGuideId { get; set; } // Identificador de workout-guide
        public string? ImagenPrincipalUrl { get; set; }
        public bool Activo { get; set; }
    }
}
