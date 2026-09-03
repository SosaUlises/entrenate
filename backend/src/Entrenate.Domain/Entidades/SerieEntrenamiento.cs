namespace Entrenate.Domain.Entidades
{
    public class SerieEntrenamiento
    {
        public Guid Id { get; set; }

        public Guid EjercicioSesionId { get; set; }

        public int NumeroSerie { get; private set; }

        public decimal Peso { get; private set; }

        public int Repeticiones { get; private set; }

        public int? Rir { get; private set; }

        public bool Completada { get; private set; }

        public DateTime FechaHoraRegistro { get; private set; }

        public EjercicioSesion EjercicioSesion { get; set; } = null!;

        public void ConfigurarNumeroSerie(int numeroSerie)
        {
            if (numeroSerie < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(numeroSerie),
                    "El número de serie debe ser mayor que cero.");

            NumeroSerie = numeroSerie;
        }

        public void RegistrarRendimiento(
            decimal peso,
            int repeticiones,
            int? rir,
            DateTime fechaHoraRegistro)
        {
            if (peso < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(peso),
                    "El peso no puede ser negativo.");

            if (peso > 1000)
                throw new ArgumentOutOfRangeException(
                    nameof(peso),
                    "El peso no puede superar los 1000 kg.");

            if (repeticiones < 1 || repeticiones > 200)
                throw new ArgumentOutOfRangeException(
                    nameof(repeticiones),
                    "Las repeticiones deben estar entre 1 y 200.");

            if (rir.HasValue && (rir.Value < 0 || rir.Value > 5))
                throw new ArgumentOutOfRangeException(
                    nameof(rir),
                    "El RIR debe estar entre 0 y 5.");

            Peso = peso;
            Repeticiones = repeticiones;
            Rir = rir;
            FechaHoraRegistro = fechaHoraRegistro;
        }

        public void MarcarComoCompletada()
        {
            if (Repeticiones <= 0)
                throw new InvalidOperationException(
                    "No se puede completar una serie sin registrar repeticiones.");

            Completada = true;
        }
    }
}
