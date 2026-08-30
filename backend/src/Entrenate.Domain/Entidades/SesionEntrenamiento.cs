using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class SesionEntrenamiento
    {
        public Guid Id { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public Guid? DiaRutinaId { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime HoraInicio { get; private set; }

        public DateTime? HoraFin { get; private set; }

        public EstadoSesionEntrenamiento Estado { get; private set; }

        public string? Notas { get; set; }

        public DiaRutina? DiaRutina { get; set; }

        public ICollection<EjercicioSesion> Ejercicios { get; set; }
            = new List<EjercicioSesion>();

        public void Iniciar(DateTime fechaHora)
        {
            HoraInicio = fechaHora;
            Fecha = fechaHora.Date;
            Estado = EstadoSesionEntrenamiento.EnCurso;
        }

        public void Completar(DateTime fechaHoraFin)
        {
            if (Estado != EstadoSesionEntrenamiento.EnCurso)
                throw new InvalidOperationException(
                    "Solo una sesión en curso puede ser completada.");

            if (fechaHoraFin < HoraInicio)
                throw new ArgumentException(
                    "La hora de finalización no puede ser anterior al inicio.");

            HoraFin = fechaHoraFin;
            Estado = EstadoSesionEntrenamiento.Completada;
        }

        public void Cancelar(DateTime fechaHoraFin)
        {
            if (Estado != EstadoSesionEntrenamiento.EnCurso)
                throw new InvalidOperationException(
                    "Solo una sesión en curso puede ser cancelada.");

            if (fechaHoraFin < HoraInicio)
                throw new ArgumentException(
                    "La hora de finalización no puede ser anterior al inicio.");

            HoraFin = fechaHoraFin;
            Estado = EstadoSesionEntrenamiento.Cancelada;
        }

        public bool EstaEnCurso()
        {
            return Estado == EstadoSesionEntrenamiento.EnCurso;
        }
    }
}
