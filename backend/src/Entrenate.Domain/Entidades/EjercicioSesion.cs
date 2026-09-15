namespace Entrenate.Domain.Entidades
{
    public class EjercicioSesion
    {
        private readonly List<SerieEntrenamiento> _series = [];

        public Guid Id { get; private set; }

        public Guid SesionEntrenamientoId { get; private set; }

        public Guid EjercicioId { get; private set; }

        public int Orden { get; private set; }

        public int SeriesObjetivo { get; private set; }

        public int RepeticionesMinimasObjetivo { get; private set; }

        public int RepeticionesMaximasObjetivo { get; private set; }

        public int RirObjetivoMinimo { get; private set; }

        public int RirObjetivoMaximo { get; private set; }

        public int DescansoObjetivoSegundos { get; private set; }

        public string? Notas { get; private set; }

        public SesionEntrenamiento SesionEntrenamiento { get; private set; }
            = null!;

        public Ejercicio Ejercicio { get; private set; } = null!;

        public IReadOnlyCollection<SerieEntrenamiento> Series
            => _series.AsReadOnly();

        private EjercicioSesion()
        {
        }

        internal EjercicioSesion(EjercicioRutina ejercicioRutina)
        {
            ArgumentNullException.ThrowIfNull(ejercicioRutina);

            EjercicioId = ejercicioRutina.EjercicioId;
            Orden = ejercicioRutina.Orden;
            SeriesObjetivo = ejercicioRutina.CantidadSeries;
            RepeticionesMinimasObjetivo =
                ejercicioRutina.RepeticionesMinimas;
            RepeticionesMaximasObjetivo =
                ejercicioRutina.RepeticionesMaximas;
            RirObjetivoMinimo = ejercicioRutina.RirObjetivoMinimo;
            RirObjetivoMaximo = ejercicioRutina.RirObjetivoMaximo;
            DescansoObjetivoSegundos = ejercicioRutina.DescansoSegundos;
            Notas = ejercicioRutina.Notas;
        }
    }
}
