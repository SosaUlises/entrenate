namespace Entrenate.Domain.Entidades
{
    public class Ejercicio
    {
        private readonly List<EjercicioEquipamiento> _equipamientos = [];

        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string GrupoMuscularPrincipal { get; set; } = string.Empty; // luego cambiarlo de string a GrupoMuscular class, verificar que recibimos de la API
        public string? WorkoutGuideId { get; set; } // Identificador de workout-guide
        public string? ImagenPrincipalUrl { get; set; }
        public bool Activo { get; set; }

        public IReadOnlyCollection<EjercicioEquipamiento> Equipamientos
            => _equipamientos.AsReadOnly();

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
    }
}
