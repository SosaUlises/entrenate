using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class SesionEntrenamiento
    {
        private readonly List<EjercicioSesion> _ejercicios = [];

        public Guid Id { get; private set; }

        public string UsuarioId { get; private set; } = string.Empty;

        public Guid? DiaRutinaId { get; private set; }

        public DateTime Fecha { get; private set; }

        public DateTime HoraInicio { get; private set; }

        public DateTime? HoraFin { get; private set; }

        public EstadoSesionEntrenamiento Estado { get; private set; }

        public string? Notas { get; private set; }

        public DiaRutina? DiaRutina { get; private set; }

        public IReadOnlyCollection<EjercicioSesion> Ejercicios
            => _ejercicios.AsReadOnly();

        private SesionEntrenamiento()
        {
        }

        public SesionEntrenamiento(
            string usuarioId,
            Guid diaRutinaId,
            DateTime fechaHoraInicio,
            IEnumerable<EjercicioRutina> ejerciciosRutina)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new ArgumentException(
                    "El usuario es obligatorio.",
                    nameof(usuarioId));
            }

            if (diaRutinaId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El día de rutina es obligatorio.",
                    nameof(diaRutinaId));
            }

            if (fechaHoraInicio.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "La fecha y hora de inicio debe estar expresada en UTC.",
                    nameof(fechaHoraInicio));
            }

            ArgumentNullException.ThrowIfNull(ejerciciosRutina);

            UsuarioId = usuarioId;
            DiaRutinaId = diaRutinaId;
            HoraInicio = fechaHoraInicio;
            Fecha = fechaHoraInicio.Date;
            HoraFin = null;
            Estado = EstadoSesionEntrenamiento.EnCurso;

            foreach (var ejercicioRutina in ejerciciosRutina
                .OrderBy(x => x.Orden))
            {
                _ejercicios.Add(new EjercicioSesion(ejercicioRutina));
            }
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
