using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class PerfilEntrenamiento
    {
        public Guid Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public NivelExperiencia NivelExperiencia { get; set; }

        public ObjetivoEntrenamiento ObjetivoPrincipal { get; set; }

        public int DiasDisponiblesSemana { get; private set; }

        public int DuracionSesionDeseadaMinutos { get; private set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime FechaUltimaModificacion { get; set; }

        public void ConfigurarDiasDisponibles(int dias)
        {
            if (dias < 1 || dias > 7)
                throw new ArgumentOutOfRangeException(
                    nameof(dias),
                    "Los días disponibles deben estar entre 1 y 7.");

            DiasDisponiblesSemana = dias;
        }

        public void ConfigurarDuracionSesion(int minutos)
        {
            if (minutos < 15 || minutos > 240)
                throw new ArgumentOutOfRangeException(
                    nameof(minutos),
                    "La duración de la sesión debe estar entre 15 y 240 minutos.");

            DuracionSesionDeseadaMinutos = minutos;
        }
    }
}
