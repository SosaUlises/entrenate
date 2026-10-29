namespace Entrenate.Domain.Entidades
{
    public class SesionAdaptacionEquipamientoDisponible
    {
        public Guid Id { get; private set; }

        public Guid SesionAdaptacionId { get; private set; }

        public Guid EquipamientoId { get; private set; }

        public SesionAdaptacion SesionAdaptacion { get; private set; } = null!;

        public Equipamiento? Equipamiento { get; private set; }

        private SesionAdaptacionEquipamientoDisponible()
        {
        }

        internal SesionAdaptacionEquipamientoDisponible(
            Guid sesionAdaptacionId,
            Guid equipamientoId)
        {
            if (sesionAdaptacionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id de la sesión de adaptación es obligatorio.",
                    nameof(sesionAdaptacionId));
            }

            if (equipamientoId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id del equipamiento es obligatorio.",
                    nameof(equipamientoId));
            }

            Id = Guid.NewGuid();
            SesionAdaptacionId = sesionAdaptacionId;
            EquipamientoId = equipamientoId;
        }
    }
}

