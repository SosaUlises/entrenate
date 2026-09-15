namespace Entrenate.Domain.Entidades
{
    public class EjercicioEquipamiento
    {
        public Guid Id { get; private set; }

        public Guid EjercicioId { get; private set; }

        public Guid EquipamientoId { get; private set; }

        private EjercicioEquipamiento()
        {
        }

        internal EjercicioEquipamiento(
            Guid ejercicioId,
            Guid equipamientoId)
        {
            Id = Guid.NewGuid();
            EjercicioId = ejercicioId;
            EquipamientoId = equipamientoId;
        }
    }
}
