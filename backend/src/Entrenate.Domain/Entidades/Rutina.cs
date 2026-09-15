namespace Entrenate.Domain.Entidades
{
    public class Rutina
    {
        private readonly List<DiaRutina> _dias = [];

        public Guid Id { get; private set; }

        public string UsuarioId { get; private set; } = string.Empty;

        public string Nombre { get; private set; } = string.Empty;

        public string? Descripcion { get; private set; }

        public DateTime FechaCreacion { get; private set; }

        public DateTime FechaUltimaModificacion { get; private set; }

        public bool Activa { get; private set; }

        public IReadOnlyCollection<DiaRutina> Dias
            => _dias.AsReadOnly();

        private Rutina()
        {
        }

        public Rutina(
            string usuarioId,
            string nombre,
            string? descripcion)
        {
            UsuarioId = usuarioId;
            Nombre = nombre.Trim();
            Descripcion = descripcion?.Trim();
            FechaCreacion = DateTime.UtcNow;
            FechaUltimaModificacion = FechaCreacion;
            Activa = true;
        }

        public void Actualizar(
            string nombre,
            string? descripcion)
        {
            Nombre = nombre.Trim();
            Descripcion = descripcion?.Trim();
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public DiaRutina AgregarDia(
            string nombre,
            string? descripcion,
            int orden)
        {
            var dia = new DiaRutina(
                Id,
                nombre,
                descripcion,
                orden);

            _dias.Add(dia);

            return dia;
        }

        public void EliminarDiasExcepto(
            IEnumerable<int> ordenes)
        {
            ArgumentNullException.ThrowIfNull(ordenes);

            var ordenesSet = ordenes.ToHashSet();

            _dias.RemoveAll(x => !ordenesSet.Contains(x.Orden));
        }
    }
}
