using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class SesionAdaptacionCambio
    {
        public Guid Id { get; private set; }

        public Guid SesionAdaptacionId { get; private set; }

        public int Orden { get; private set; }

        public TipoCambioAdaptacionSesion TipoCambio { get; private set; }

        public MotivoSustitucionAdaptacion? Motivo { get; private set; }

        public Guid? SourceRoutineExerciseId { get; private set; }

        public Guid? EjercicioOriginalId { get; private set; }

        public string? EjercicioOriginalCodigo { get; private set; }

        public Guid? EjercicioAdaptadoId { get; private set; }

        public string? EjercicioAdaptadoCodigo { get; private set; }

        public int? SeriesOriginal { get; private set; }

        public int? SeriesAdaptada { get; private set; }

        public int? RirMinimoOriginal { get; private set; }

        public int? RirMaximoOriginal { get; private set; }

        public int? RirMinimoAdaptado { get; private set; }

        public int? RirMaximoAdaptado { get; private set; }

        public SesionAdaptacion SesionAdaptacion { get; private set; } = null!;

        public Ejercicio? EjercicioOriginal { get; private set; }

        public Ejercicio? EjercicioAdaptado { get; private set; }

        private SesionAdaptacionCambio()
        {
        }

        internal SesionAdaptacionCambio(
            Guid sesionAdaptacionId,
            int orden,
            TipoCambioAdaptacionSesion tipoCambio,
            Guid? sourceRoutineExerciseId = null,
            Guid? ejercicioOriginalId = null,
            string? ejercicioOriginalCodigo = null,
            Guid? ejercicioAdaptadoId = null,
            string? ejercicioAdaptadoCodigo = null,
            int? seriesOriginal = null,
            int? seriesAdaptada = null,
            int? rirMinimoOriginal = null,
            int? rirMaximoOriginal = null,
            int? rirMinimoAdaptado = null,
            int? rirMaximoAdaptado = null,
            MotivoSustitucionAdaptacion? motivo = null)
        {
            if (sesionAdaptacionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id de la sesión de adaptación es obligatorio.",
                    nameof(sesionAdaptacionId));
            }

            if (orden < 1)
            {
                throw new ArgumentException(
                    "El orden del cambio debe ser mayor o igual a 1.",
                    nameof(orden));
            }

            if (!Enum.IsDefined(tipoCambio))
            {
                throw new ArgumentException(
                    "El tipo de cambio no es válido.",
                    nameof(tipoCambio));
            }

            Id = Guid.NewGuid();
            SesionAdaptacionId = sesionAdaptacionId;
            Orden = orden;
            TipoCambio = tipoCambio;
            SourceRoutineExerciseId = sourceRoutineExerciseId;
            EjercicioOriginalId = ejercicioOriginalId;
            EjercicioOriginalCodigo = ejercicioOriginalCodigo?.Trim().ToLowerInvariant();
            EjercicioAdaptadoId = ejercicioAdaptadoId;
            EjercicioAdaptadoCodigo = ejercicioAdaptadoCodigo?.Trim().ToLowerInvariant();
            SeriesOriginal = seriesOriginal;
            SeriesAdaptada = seriesAdaptada;
            RirMinimoOriginal = rirMinimoOriginal;
            RirMaximoOriginal = rirMaximoOriginal;
            RirMinimoAdaptado = rirMinimoAdaptado;
            RirMaximoAdaptado = rirMaximoAdaptado;
            Motivo = motivo;
        }
    }
}

