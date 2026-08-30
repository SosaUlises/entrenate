namespace Entrenate.Domain.Entidades
{
    public class DiaRutina
    {
        public Guid Id { get; set; }

        public Guid RutinaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public int Orden { get; set; }

        public Rutina Rutina { get; set; } = null!;

        public ICollection<EjercicioRutina> Ejercicios { get; set; }
            = new List<EjercicioRutina>();
    }
}
