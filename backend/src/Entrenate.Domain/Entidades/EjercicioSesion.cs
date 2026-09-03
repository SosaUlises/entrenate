namespace Entrenate.Domain.Entidades
{
    // Snapshot de un ejercicio dentro de una sesión de entrenamiento. Contiene los objetivos de series, repeticiones, RIR y descanso para ese ejercicio en particular. Util para historial lo que se hizo aunque se modifiquen los objetivos en la rutina.
    public class EjercicioSesion
    {
        public Guid Id { get; set; }

        public Guid SesionEntrenamientoId { get; set; }

        public Guid EjercicioId { get; set; }

        public int Orden { get; set; }

        public int SeriesObjetivo { get; set; }

        public int RepeticionesMinimasObjetivo { get; set; }

        public int RepeticionesMaximasObjetivo { get; set; }

        public int RirObjetivoMinimo { get; set; }

        public int RirObjetivoMaximo { get; set; }

        public int DescansoObjetivoSegundos { get; set; }

        public string? Notas { get; set; }

        public SesionEntrenamiento SesionEntrenamiento { get; set; } = null!;

        public Ejercicio Ejercicio { get; set; } = null!;

        public ICollection<SerieEntrenamiento> Series { get; set; }
            = new List<SerieEntrenamiento>();
    }
}
