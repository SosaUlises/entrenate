namespace Entrenate.Domain.Entidades
{
    public class Rutina
    {
        public Guid Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaUltimaModificacion { get; set; }

        public bool Activa { get; set; }

        public ICollection<DiaRutina> Dias { get; set; } = new List<DiaRutina>();
    }
}
