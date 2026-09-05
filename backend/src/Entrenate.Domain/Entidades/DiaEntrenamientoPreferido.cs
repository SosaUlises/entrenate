using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class DiaEntrenamientoPreferido
    {
        public Guid Id { get; private set; }

        public Guid PerfilEntrenamientoId { get; private set; }

        public DiaSemana Dia { get; private set; }

        private DiaEntrenamientoPreferido()
        {
        }

        internal DiaEntrenamientoPreferido(
            Guid perfilEntrenamientoId,
            DiaSemana dia)
        {
            Id = Guid.NewGuid();
            PerfilEntrenamientoId = perfilEntrenamientoId;
            Dia = dia;
        }
    }
}
