namespace Entrenate.Domain.Entidades
{
    public class Ejercicio
    {
        private readonly List<EjercicioEquipamiento> _equipamientos = [];
        private readonly List<EjercicioGrupoMuscularSecundario>
            _gruposMuscularesSecundarios = [];

        public Guid Id { get; set; }
        public string? Codigo { get; private set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string GrupoMuscularPrincipal { get; set; } = string.Empty;
        public Entrenate.Domain.Enums.PatronMovimiento? PatronMovimiento
        {
            get;
            private set;
        }

        public Entrenate.Domain.Enums.TipoEjercicio? TipoEjercicio
        {
            get;
            private set;
        }
        public string? WorkoutGuideId { get; set; }
        public string? ImagenPrincipalUrl { get; set; }
        public bool Activo { get; set; }

        public IReadOnlyCollection<EjercicioEquipamiento> Equipamientos
            => _equipamientos.AsReadOnly();

        public IReadOnlyCollection<EjercicioGrupoMuscularSecundario>
            GruposMuscularesSecundarios
            => _gruposMuscularesSecundarios.AsReadOnly();

        public void ConfigurarMetadataPlanificacion(
            string codigo,
            Entrenate.Domain.Enums.PatronMovimiento patronMovimiento,
            Entrenate.Domain.Enums.TipoEjercicio tipoEjercicio,
            IEnumerable<string> gruposMuscularesSecundarios)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException(
                    "El código estable del ejercicio es obligatorio.",
                    nameof(codigo));
            }

            ArgumentNullException.ThrowIfNull(
                gruposMuscularesSecundarios);

            Codigo = codigo.Trim().ToLowerInvariant();
            PatronMovimiento = patronMovimiento;
            TipoEjercicio = tipoEjercicio;

            DefinirGruposMuscularesSecundarios(
                gruposMuscularesSecundarios);
        }

        public void DefinirEquipamientos(
            IEnumerable<Guid> equipamientoIds)
        {
            ArgumentNullException.ThrowIfNull(equipamientoIds);

            var idsUnicos = equipamientoIds
                .Distinct()
                .ToHashSet();

            _equipamientos.RemoveAll(
                actual =>
                    !idsUnicos.Contains(actual.EquipamientoId));

            var idsExistentes = _equipamientos
                .Select(x => x.EquipamientoId)
                .ToHashSet();

            foreach (var equipamientoId in idsUnicos)
            {
                if (idsExistentes.Contains(equipamientoId))
                {
                    continue;
                }

                _equipamientos.Add(
                    new EjercicioEquipamiento(
                        Id,
                        equipamientoId));
            }
        }

        private void DefinirGruposMuscularesSecundarios(
            IEnumerable<string> gruposMusculares)
        {
            var gruposUnicos = gruposMusculares
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (gruposUnicos.Any(x => string.Equals(
                x,
                GrupoMuscularPrincipal,
                StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    "El grupo muscular principal no puede repetirse como " +
                        "secundario.",
                    nameof(gruposMusculares));
            }

            _gruposMuscularesSecundarios.RemoveAll(
                actual => !gruposUnicos.Contains(
                    actual.GrupoMuscular,
                    StringComparer.OrdinalIgnoreCase));

            var existentes = _gruposMuscularesSecundarios
                .Select(x => x.GrupoMuscular)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var grupo in gruposUnicos)
            {
                if (existentes.Contains(grupo))
                {
                    continue;
                }

                _gruposMuscularesSecundarios.Add(
                    new EjercicioGrupoMuscularSecundario(
                        Id,
                        grupo));
            }
        }
    }
}
