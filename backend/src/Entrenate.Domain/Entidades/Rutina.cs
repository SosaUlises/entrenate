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

        public Guid? PlannerProposalId { get; private set; }

        public string? PlannerProposalHash { get; private set; }

        public IReadOnlyCollection<DiaRutina> Dias
            => _dias.AsReadOnly();

        private Rutina()
        {
        }

        public Rutina(
            string usuarioId,
            string nombre,
            string? descripcion,
            Guid? plannerProposalId = null,
            string? plannerProposalHash = null)
        {
            var normalizedProposalHash = string.IsNullOrWhiteSpace(
                plannerProposalHash)
                ? null
                : plannerProposalHash.Trim().ToLowerInvariant();

            if (plannerProposalId.HasValue !=
                (normalizedProposalHash is not null))
            {
                throw new ArgumentException(
                    "La identidad y el hash de la propuesta deben " +
                        "informarse juntos.");
            }

            if (plannerProposalId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El identificador de la propuesta no es válido.",
                    nameof(plannerProposalId));
            }

            if (normalizedProposalHash is not null &&
                (normalizedProposalHash.Length != 64 ||
                    normalizedProposalHash.Any(x => !Uri.IsHexDigit(x))))
            {
                throw new ArgumentException(
                    "El hash de la propuesta debe ser SHA-256.",
                    nameof(plannerProposalHash));
            }

            UsuarioId = usuarioId;
            Nombre = nombre.Trim();
            Descripcion = descripcion?.Trim();
            FechaCreacion = DateTime.UtcNow;
            FechaUltimaModificacion = FechaCreacion;
            Activa = true;
            PlannerProposalId = plannerProposalId;
            PlannerProposalHash = normalizedProposalHash;
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
            int orden,
            Entrenate.Domain.Enums.DiaSemana? diaSemana = null)
        {
            var dia = new DiaRutina(
                Id,
                nombre,
                descripcion,
                orden,
                diaSemana);

            _dias.Add(dia);

            return dia;
        }

        public void Activar()
        {
            Activa = true;
            FechaUltimaModificacion = DateTime.UtcNow;
        }

        public void Desactivar()
        {
            Activa = false;
            FechaUltimaModificacion = DateTime.UtcNow;
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
