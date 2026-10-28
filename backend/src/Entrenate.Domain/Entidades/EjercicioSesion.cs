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

        public EjercicioSesion(
            Guid ejercicioId,
            int orden,
            int seriesObjetivo,
            int repeticionesMinimasObjetivo,
            int repeticionesMaximasObjetivo,
            int rirObjetivoMinimo,
            int rirObjetivoMaximo,
            int descansoObjetivoSegundos,
            string? notas = null)
        {
            if (ejercicioId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El ejercicio es obligatorio.",
                    nameof(ejercicioId));
            }

            if (orden < 1)
            {
                throw new ArgumentException(
                    "El orden debe ser mayor o igual a 1.",
                    nameof(orden));
            }

            if (seriesObjetivo < 1)
            {
                throw new ArgumentException(
                    "La cantidad de series debe ser mayor o igual a 1.",
                    nameof(seriesObjetivo));
            }

            if (repeticionesMinimasObjetivo < 1)
            {
                throw new ArgumentException(
                    "Las repeticiones mínimas deben ser mayores o iguales a 1.",
                    nameof(repeticionesMinimasObjetivo));
            }

            if (repeticionesMaximasObjetivo < repeticionesMinimasObjetivo)
            {
                throw new ArgumentException(
                    "Las repeticiones máximas no pueden ser menores a las mínimas.",
                    nameof(repeticionesMaximasObjetivo));
            }

            if (rirObjetivoMinimo < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rirObjetivoMinimo),
                    "El RIR mínimo debe ser mayor o igual a 0.");
            }

            if (rirObjetivoMaximo < rirObjetivoMinimo)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rirObjetivoMaximo),
                    "El RIR máximo no puede ser menor al RIR mínimo.");
            }

            if (descansoObjetivoSegundos < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(descansoObjetivoSegundos),
                    "El descanso objetivo no puede ser negativo.");
            }

            Id = Guid.NewGuid();
            EjercicioId = ejercicioId;
            Orden = orden;
            SeriesObjetivo = seriesObjetivo;
            RepeticionesMinimasObjetivo = repeticionesMinimasObjetivo;
            RepeticionesMaximasObjetivo = repeticionesMaximasObjetivo;
            RirObjetivoMinimo = rirObjetivoMinimo;
            RirObjetivoMaximo = rirObjetivoMaximo;
            DescansoObjetivoSegundos = descansoObjetivoSegundos;
            Notas = notas;
        }

        public SerieEntrenamiento RegistrarOActualizarSerie(
            int numeroSerie,
            decimal peso,
            int repeticiones,
            int? rir,
            DateTime fechaHoraRegistro)
        {
            var serie = _series.FirstOrDefault(
                x => x.NumeroSerie == numeroSerie);

            if (serie is null)
            {
                serie = new SerieEntrenamiento();
                serie.ConfigurarNumeroSerie(numeroSerie);
                serie.RegistrarRendimiento(
                    peso,
                    repeticiones,
                    rir,
                    fechaHoraRegistro);
                serie.MarcarComoCompletada();

                _series.Add(serie);

                return serie;
            }

            serie.RegistrarRendimiento(
                peso,
                repeticiones,
                rir,
                fechaHoraRegistro);

            return serie;
        }
    }
}
