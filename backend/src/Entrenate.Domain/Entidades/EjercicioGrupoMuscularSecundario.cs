namespace Entrenate.Domain.Entidades
{
    public class EjercicioGrupoMuscularSecundario
    {
        public Guid Id { get; private set; }

        public Guid EjercicioId { get; private set; }

        public string GrupoMuscular { get; private set; } = string.Empty;

        private EjercicioGrupoMuscularSecundario()
        {
        }

        internal EjercicioGrupoMuscularSecundario(
            Guid ejercicioId,
            string grupoMuscular)
        {
            Id = Guid.NewGuid();
            EjercicioId = ejercicioId;
            GrupoMuscular = grupoMuscular.Trim();
        }
    }
}
