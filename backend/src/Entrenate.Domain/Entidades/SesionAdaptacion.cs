using Entrenate.Domain.Enums;

namespace Entrenate.Domain.Entidades
{
    public class SesionAdaptacion
    {
        private readonly List<SesionAdaptacionCambio> _cambios = [];
        private readonly List<SesionAdaptacionEquipamientoDisponible> _equipamientosDisponibles = [];
        private readonly List<SesionAdaptacionEjercicioExcluido> _ejerciciosExcluidos = [];

        public Guid Id { get; private set; }

        public Guid SesionEntrenamientoId { get; private set; }

        public string UsuarioId { get; private set; } = string.Empty;

        public string PolicyVersion { get; private set; } = string.Empty;

        public string AdaptationProposalId { get; private set; } = string.Empty;

        public string ProposalHash { get; private set; } = string.Empty;

        public int? AvailableMinutes { get; private set; }

        public PreferenciaEsfuerzoSesion EffortPreference { get; private set; }

        public FactibilidadAdaptacionSesion Factibilidad { get; private set; }

        public DateTime FechaConfirmacionUtc { get; private set; }

        public SesionEntrenamiento SesionEntrenamiento { get; private set; } = null!;

        public IReadOnlyCollection<SesionAdaptacionCambio> Cambios
            => _cambios.AsReadOnly();

        public IReadOnlyCollection<SesionAdaptacionEquipamientoDisponible> EquipamientosDisponibles
            => _equipamientosDisponibles.AsReadOnly();

        public IReadOnlyCollection<SesionAdaptacionEjercicioExcluido> EjerciciosExcluidos
            => _ejerciciosExcluidos.AsReadOnly();

        private SesionAdaptacion()
        {
        }

        public SesionAdaptacion(
            Guid sesionEntrenamientoId,
            string usuarioId,
            string adaptationProposalId,
            string proposalHash,
            string policyVersion,
            FactibilidadAdaptacionSesion factibilidad,
            PreferenciaEsfuerzoSesion effortPreference,
            int? availableMinutes,
            DateTime fechaConfirmacionUtc,
            IEnumerable<Guid>? equipamientosDisponibles = null,
            IEnumerable<Guid>? ejerciciosExcluidos = null)
        {
            if (sesionEntrenamientoId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id de la sesión de entrenamiento es obligatorio.",
                    nameof(sesionEntrenamientoId));
            }

            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                throw new ArgumentException(
                    "El id del usuario es obligatorio.",
                    nameof(usuarioId));
            }

            if (string.IsNullOrWhiteSpace(adaptationProposalId))
            {
                throw new ArgumentException(
                    "El id de propuesta de adaptación es obligatorio.",
                    nameof(adaptationProposalId));
            }

            if (string.IsNullOrWhiteSpace(proposalHash))
            {
                throw new ArgumentException(
                    "El hash de propuesta de adaptación es obligatorio.",
                    nameof(proposalHash));
            }

            if (string.IsNullOrWhiteSpace(policyVersion))
            {
                throw new ArgumentException(
                    "La versión de política es obligatoria.",
                    nameof(policyVersion));
            }

            if (fechaConfirmacionUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException(
                    "La fecha de confirmación debe estar expresada en UTC.",
                    nameof(fechaConfirmacionUtc));
            }

            if (availableMinutes.HasValue && availableMinutes.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(availableMinutes),
                    "El tiempo disponible debe ser un valor positivo.");
            }

            if (!Enum.IsDefined(factibilidad))
            {
                throw new ArgumentException(
                    "La factibilidad indicada no es válida.",
                    nameof(factibilidad));
            }

            if (!Enum.IsDefined(effortPreference))
            {
                throw new ArgumentException(
                    "La preferencia de esfuerzo no es válida.",
                    nameof(effortPreference));
            }

            Id = Guid.NewGuid();
            SesionEntrenamientoId = sesionEntrenamientoId;
            UsuarioId = usuarioId.Trim();
            AdaptationProposalId = adaptationProposalId.Trim();
            ProposalHash = proposalHash.Trim();
            PolicyVersion = policyVersion.Trim();
            Factibilidad = factibilidad;
            EffortPreference = effortPreference;
            AvailableMinutes = availableMinutes;
            FechaConfirmacionUtc = fechaConfirmacionUtc;

            if (equipamientosDisponibles != null)
            {
                foreach (var eqId in equipamientosDisponibles)
                {
                    RegistrarEquipamientoDisponible(eqId);
                }
            }

            if (ejerciciosExcluidos != null)
            {
                foreach (var ejId in ejerciciosExcluidos)
                {
                    RegistrarEjercicioExcluido(ejId);
                }
            }
        }

        public void RegistrarEquipamientoDisponible(Guid equipamientoId)
        {
            if (equipamientoId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id de equipamiento no puede ser vacío.",
                    nameof(equipamientoId));
            }

            if (_equipamientosDisponibles.Any(x => x.EquipamientoId == equipamientoId))
            {
                return;
            }

            _equipamientosDisponibles.Add(
                new SesionAdaptacionEquipamientoDisponible(Id, equipamientoId));
        }

        public void RegistrarEjercicioExcluido(Guid ejercicioId)
        {
            if (ejercicioId == Guid.Empty)
            {
                throw new ArgumentException(
                    "El id de ejercicio no puede ser vacío.",
                    nameof(ejercicioId));
            }

            if (_ejerciciosExcluidos.Any(x => x.EjercicioId == ejercicioId))
            {
                return;
            }

            _ejerciciosExcluidos.Add(
                new SesionAdaptacionEjercicioExcluido(Id, ejercicioId));
        }

        public SesionAdaptacionCambio AgregarCambio(
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
            var cambio = new SesionAdaptacionCambio(
                Id,
                _cambios.Count + 1,
                tipoCambio,
                sourceRoutineExerciseId,
                ejercicioOriginalId,
                ejercicioOriginalCodigo,
                ejercicioAdaptadoId,
                ejercicioAdaptadoCodigo,
                seriesOriginal,
                seriesAdaptada,
                rirMinimoOriginal,
                rirMaximoOriginal,
                rirMinimoAdaptado,
                rirMaximoAdaptado,
                motivo);

            _cambios.Add(cambio);
            return cambio;
        }
    }
}
