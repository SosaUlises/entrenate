using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class AdaptacionLiveCoachSesion
    {
        public Guid Id { get; private set; }
        public Guid SesionEntrenamientoId { get; private set; }
        public TipoAdaptacionLiveCoach Tipo { get; private set; }
        public DateTime FechaAplicacionUtc { get; private set; }
        public Guid? EjercicioSesionId { get; private set; }
        public int? SerieEfectivaDesde { get; private set; }
        public int? RepeticionesMinimasAnteriores { get; private set; }
        public int? RepeticionesMaximasAnteriores { get; private set; }
        public int? RepeticionesMinimasNuevas { get; private set; }
        public int? RepeticionesMaximasNuevas { get; private set; }
        public int? RirMinimoAnterior { get; private set; }
        public int? RirMaximoAnterior { get; private set; }
        public int? RirMinimoNuevo { get; private set; }
        public int? RirMaximoNuevo { get; private set; }
        public SesionEntrenamiento SesionEntrenamiento { get; private set; } = null!;

        private AdaptacionLiveCoachSesion() { }

        internal AdaptacionLiveCoachSesion(
            Guid sesionEntrenamientoId,
            TipoAdaptacionLiveCoach tipo,
            DateTime fechaAplicacionUtc,
            Guid? ejercicioSesionId = null,
            int? serieEfectivaDesde = null,
            int? repeticionesMinimasAnteriores = null,
            int? repeticionesMaximasAnteriores = null,
            int? repeticionesMinimasNuevas = null,
            int? repeticionesMaximasNuevas = null,
            int? rirMinimoAnterior = null,
            int? rirMaximoAnterior = null,
            int? rirMinimoNuevo = null,
            int? rirMaximoNuevo = null,
            Guid? id = null)
        {
            if (sesionEntrenamientoId == Guid.Empty)
                throw new ArgumentException("La sesion es obligatoria.", nameof(sesionEntrenamientoId));
            if (fechaAplicacionUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("La fecha debe estar expresada en UTC.", nameof(fechaAplicacionUtc));

            if (id.HasValue && id.Value != Guid.Empty)
            {
                Id = id.Value;
            }
            SesionEntrenamientoId = sesionEntrenamientoId;
            Tipo = tipo;
            FechaAplicacionUtc = fechaAplicacionUtc;
            EjercicioSesionId = ejercicioSesionId;
            SerieEfectivaDesde = serieEfectivaDesde;
            RepeticionesMinimasAnteriores = repeticionesMinimasAnteriores;
            RepeticionesMaximasAnteriores = repeticionesMaximasAnteriores;
            RepeticionesMinimasNuevas = repeticionesMinimasNuevas;
            RepeticionesMaximasNuevas = repeticionesMaximasNuevas;
            RirMinimoAnterior = rirMinimoAnterior;
            RirMaximoAnterior = rirMaximoAnterior;
            RirMinimoNuevo = rirMinimoNuevo;
            RirMaximoNuevo = rirMaximoNuevo;
        }
    }
}
