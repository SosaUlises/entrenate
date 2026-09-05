namespace Entrenate.Domain.Entidades
{
    public class PerfilEquipamiento
    {
        public Guid Id { get; private set; }

        public Guid PerfilEntrenamientoId { get; private set; }

        public Guid EquipamientoId { get; private set; }

        private PerfilEquipamiento()
        {
        }

        internal PerfilEquipamiento(
            Guid perfilEntrenamientoId,
            Guid equipamientoId)
        {
            Id = Guid.NewGuid();
            PerfilEntrenamientoId = perfilEntrenamientoId;
            EquipamientoId = equipamientoId;
        }
    }
}
