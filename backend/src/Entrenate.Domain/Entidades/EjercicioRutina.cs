namespace Entrenate.Domain.Entidades
{
    public class EjercicioRutina
    {
        public Guid Id { get; set; }

        public Guid DiaRutinaId { get; set; }

        public Guid EjercicioId { get; set; }

        public int CantidadSeries { get; private set; }

        public int RepeticionesMinimas { get; private set; }

        public int RepeticionesMaximas { get; private set; }

        public int RirObjetivoMinimo { get; private set; }

        public int RirObjetivoMaximo { get; private set; }

        public int DescansoSegundos { get; private set; }

        public int Orden { get; set; }

        public string? Notas { get; set; }

        public DiaRutina DiaRutina { get; set; } = null!;

        public Ejercicio Ejercicio { get; set; } = null!;

        public void ConfigurarSeries(int cantidad)
        {
            if (cantidad < 1 || cantidad > 20)
                throw new ArgumentOutOfRangeException(
                    nameof(cantidad),
                    "La cantidad de series debe estar entre 1 y 20.");

            CantidadSeries = cantidad;
        }

        public void ConfigurarRepeticiones(int minimo, int maximo)
        {
            if (minimo < 1 || minimo > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(minimo),
                    "Las repeticiones mínimas deben estar entre 1 y 100.");

            if (maximo < 1 || maximo > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(maximo),
                    "Las repeticiones máximas deben estar entre 1 y 100.");

            if (minimo > maximo)
                throw new ArgumentException(
                    "Las repeticiones mínimas no pueden ser mayores que las máximas.");

            RepeticionesMinimas = minimo;
            RepeticionesMaximas = maximo;
        }

        public void ConfigurarRirObjetivo(int minimo, int maximo)
        {
            if (minimo < 0 || minimo > 5)
                throw new ArgumentOutOfRangeException(
                    nameof(minimo),
                    "El RIR mínimo debe estar entre 0 y 5.");

            if (maximo < 0 || maximo > 5)
                throw new ArgumentOutOfRangeException(
                    nameof(maximo),
                    "El RIR máximo debe estar entre 0 y 5.");

            if (minimo > maximo)
                throw new ArgumentException(
                    "El RIR mínimo no puede ser mayor que el máximo.");

            RirObjetivoMinimo = minimo;
            RirObjetivoMaximo = maximo;
        }

        public void ConfigurarDescanso(int segundos)
        {
            if (segundos < 15 || segundos > 900)
                throw new ArgumentOutOfRangeException(
                    nameof(segundos),
                    "El descanso debe estar entre 15 y 900 segundos.");

            DescansoSegundos = segundos;
        }
    }
}
