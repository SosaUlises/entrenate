namespace Entrenate.Domain.Entidades
{
    public class DiaRutina
    {
        private readonly List<EjercicioRutina> _ejercicios = [];

        public Guid Id { get; private set; }

        public Guid RutinaId { get; private set; }

        public string Nombre { get; private set; } = string.Empty;

        public string? Descripcion { get; private set; }

        public int Orden { get; private set; }

        public Rutina Rutina { get; private set; } = null!;

        public IReadOnlyCollection<EjercicioRutina> Ejercicios
            => _ejercicios.AsReadOnly();

        private DiaRutina()
        {
        }

        internal DiaRutina(
            Guid rutinaId,
            string nombre,
            string? descripcion,
            int orden)
        {
            RutinaId = rutinaId;
            Nombre = nombre.Trim();
            Descripcion = descripcion?.Trim();
            Orden = orden;
        }

        public void Actualizar(
            string nombre,
            string? descripcion)
        {
            Nombre = nombre.Trim();
            Descripcion = descripcion?.Trim();
        }

        public EjercicioRutina AgregarEjercicio(
            Guid ejercicioId,
            int cantidadSeries,
            int repeticionesMinimas,
            int repeticionesMaximas,
            int rirObjetivoMinimo,
            int rirObjetivoMaximo,
            int descansoSegundos,
            int orden,
            string? notas)
        {
            var ejercicio = new EjercicioRutina(
                Id,
                ejercicioId,
                cantidadSeries,
                repeticionesMinimas,
                repeticionesMaximas,
                rirObjetivoMinimo,
                rirObjetivoMaximo,
                descansoSegundos,
                orden,
                notas);

            _ejercicios.Add(ejercicio);

            return ejercicio;
        }

        public void EliminarEjerciciosExcepto(
            IEnumerable<int> ordenes)
        {
            ArgumentNullException.ThrowIfNull(ordenes);

            var ordenesSet = ordenes.ToHashSet();

            _ejercicios.RemoveAll(
                x => !ordenesSet.Contains(x.Orden));
        }
    }
}
