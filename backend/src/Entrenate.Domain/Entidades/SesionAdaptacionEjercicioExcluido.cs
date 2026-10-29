namespace Entrenate.Domain.Entidades
{
    public class SesionAdaptacionEjercicioExcluido
    {
        public Guid Id { get; private set; }

        public Guid SesionAdaptacionId { get; private set; }

        public Guid EjercicioId { get; private set; }

        public SesionAdaptacion SesionAdaptacion { get; private set; } = null!;

        public Ejercicio? Ejercicio { get; private set; }

        private SesionAdaptacionEjercicioExcluido()
        {
        }

        internal SesionAdaptacionEjercicioExcluido(
            Guid sesionAdaptacionId,
            Guid ejercicioId)
        {
            if (sesionAdaptacionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id de la sesión de adaptación es obligatorio.",
                    nameof(sesionAdaptacionId));
            }

            if (ejercicioId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id del ejercicio es obligatorio.",
                    nameof(ejercicioId));
            }

            Id = Guid.NewGuid();
            SesionAdaptacionId = sesionAdaptacionId;
            EjercicioId = ejercicioId;
        }
    }
}

