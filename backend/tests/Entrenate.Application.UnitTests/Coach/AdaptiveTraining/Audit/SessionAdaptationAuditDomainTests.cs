using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Audit
{
    public class SessionAdaptationAuditDomainTests
    {
        [Fact]
        public void Constructor_WithValidArguments_InitializesCorrectly()
        {
            var sesionId = Guid.NewGuid();
            var usuarioId = "user-123";
            var proposalId = "prop-abc-456";
            var hash = "hash-123456789";
            var policyVersion = "V3";
            var confirmedUtc = DateTime.UtcNow;
            var availEq = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var exclEx = new[] { Guid.NewGuid() };

            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: sesionId,
                usuarioId: usuarioId,
                adaptationProposalId: proposalId,
                proposalHash: hash,
                policyVersion: policyVersion,
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Liviana,
                availableMinutes: 45,
                fechaConfirmacionUtc: confirmedUtc,
                equipamientosDisponibles: availEq,
                ejerciciosExcluidos: exclEx);

            Assert.NotEqual(Guid.Empty, adaptacion.Id);
            Assert.Equal(sesionId, adaptacion.SesionEntrenamientoId);
            Assert.Equal(usuarioId, adaptacion.UsuarioId);
            Assert.Equal(proposalId, adaptacion.AdaptationProposalId);
            Assert.Equal(hash, adaptacion.ProposalHash);
            Assert.Equal(policyVersion, adaptacion.PolicyVersion);
            Assert.Equal(FactibilidadAdaptacionSesion.FactibleConCambios, adaptacion.Factibilidad);
            Assert.Equal(PreferenciaEsfuerzoSesion.Liviana, adaptacion.EffortPreference);
            Assert.Equal(45, adaptacion.AvailableMinutes);
            Assert.Equal(confirmedUtc, adaptacion.FechaConfirmacionUtc);
            Assert.Equal(2, adaptacion.EquipamientosDisponibles.Count);
            Assert.Single(adaptacion.EjerciciosExcluidos);
            Assert.Empty(adaptacion.Cambios);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_InvalidUsuarioId_ThrowsArgumentException(string? invalidUserId)
        {
            Assert.Throws<ArgumentException>(() => new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(),
                usuarioId: invalidUserId!,
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.Factible,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: null,
                fechaConfirmacionUtc: DateTime.UtcNow));
        }

        [Fact]
        public void Constructor_EmptySesionId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SesionAdaptacion(
                sesionEntrenamientoId: Guid.Empty,
                usuarioId: "user-1",
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.Factible,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: null,
                fechaConfirmacionUtc: DateTime.UtcNow));
        }

        [Fact]
        public void Constructor_NonUtcDateTime_ThrowsArgumentException()
        {
            var localTime = DateTime.Now;
            if (localTime.Kind == DateTimeKind.Utc)
            {
                localTime = DateTime.SpecifyKind(localTime, DateTimeKind.Local);
            }

            Assert.Throws<ArgumentException>(() => new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(),
                usuarioId: "user-1",
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.Factible,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: null,
                fechaConfirmacionUtc: localTime));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Constructor_InvalidAvailableMinutes_ThrowsArgumentOutOfRangeException(int invalidMinutes)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(),
                usuarioId: "user-1",
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.Factible,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: invalidMinutes,
                fechaConfirmacionUtc: DateTime.UtcNow));
        }

        [Fact]
        public void AgregarCambio_AddsOrderedDeltas()
        {
            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(),
                usuarioId: "user-1",
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Liviana,
                availableMinutes: 30,
                fechaConfirmacionUtc: DateTime.UtcNow);

            var exOrigId = Guid.NewGuid();
            var exSubstId = Guid.NewGuid();
            var sourceExerciseId = Guid.NewGuid();

            var cambio1 = adaptacion.AgregarCambio(
                tipoCambio: TipoCambioAdaptacionSesion.EjercicioSustituido,
                sourceRoutineExerciseId: sourceExerciseId,
                ejercicioOriginalId: exOrigId,
                ejercicioOriginalCodigo: "bench-press",
                ejercicioAdaptadoId: exSubstId,
                ejercicioAdaptadoCodigo: "dumbbell-press",
                motivo: MotivoSustitucionAdaptacion.EquipamientoNoDisponible);

            var cambio2 = adaptacion.AgregarCambio(
                tipoCambio: TipoCambioAdaptacionSesion.SeriesReducidasPorTiempo,
                sourceRoutineExerciseId: sourceExerciseId,
                ejercicioOriginalId: exSubstId,
                seriesOriginal: 4,
                seriesAdaptada: 3);

            var cambio3 = adaptacion.AgregarCambio(
                tipoCambio: TipoCambioAdaptacionSesion.RirAumentado,
                sourceRoutineExerciseId: sourceExerciseId,
                ejercicioOriginalId: exSubstId,
                rirMinimoOriginal: 1,
                rirMaximoOriginal: 2,
                rirMinimoAdaptado: 2,
                rirMaximoAdaptado: 3);

            Assert.Equal(3, adaptacion.Cambios.Count);
            Assert.Equal(1, cambio1.Orden);
            Assert.Equal(2, cambio2.Orden);
            Assert.Equal(3, cambio3.Orden);
            Assert.Equal(TipoCambioAdaptacionSesion.EjercicioSustituido, cambio1.TipoCambio);
            Assert.Equal(MotivoSustitucionAdaptacion.EquipamientoNoDisponible, cambio1.Motivo);
            Assert.Equal(4, cambio2.SeriesOriginal);
            Assert.Equal(3, cambio2.SeriesAdaptada);
            Assert.Equal(1, cambio3.RirMinimoOriginal);
            Assert.Equal(2, cambio3.RirMinimoAdaptado);
        }

        [Fact]
        public void RegistrarEquipamientoDisponible_DeduplicatesIdenticalIds()
        {
            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(),
                usuarioId: "user-1",
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.Factible,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: null,
                fechaConfirmacionUtc: DateTime.UtcNow);

            var eqId = Guid.NewGuid();
            adaptacion.RegistrarEquipamientoDisponible(eqId);
            adaptacion.RegistrarEquipamientoDisponible(eqId);

            Assert.Single(adaptacion.EquipamientosDisponibles);
            Assert.Equal(eqId, adaptacion.EquipamientosDisponibles.First().EquipamientoId);
        }

        [Fact]
        public void RegistrarEjercicioExcluido_DeduplicatesIdenticalIds()
        {
            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(),
                usuarioId: "user-1",
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.Factible,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: null,
                fechaConfirmacionUtc: DateTime.UtcNow);

            var exId = Guid.NewGuid();
            adaptacion.RegistrarEjercicioExcluido(exId);
            adaptacion.RegistrarEjercicioExcluido(exId);

            Assert.Single(adaptacion.EjerciciosExcluidos);
            Assert.Equal(exId, adaptacion.EjerciciosExcluidos.First().EjercicioId);
        }

        [Fact]
        public void SesionEntrenamiento_AsignarAdaptacion_AssociatesAdaptacionCorrectly()
        {
            var sesion = new SesionEntrenamiento(
                "user-1",
                Guid.NewGuid(),
                DateTime.UtcNow,
                Enumerable.Empty<EjercicioRutina>());

            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: sesion.Id,
                usuarioId: sesion.UsuarioId,
                adaptationProposalId: "prop-123",
                proposalHash: "hash-abc",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: 45,
                fechaConfirmacionUtc: DateTime.UtcNow);

            sesion.AsignarAdaptacion(adaptacion);

            Assert.Same(adaptacion, sesion.Adaptacion);
        }

        [Fact]
        public void SesionEntrenamiento_AsignarAdaptacion_MismatchedSesionId_ThrowsInvalidOperationException()
        {
            var sesion = new SesionEntrenamiento(
                "user-1",
                Guid.NewGuid(),
                DateTime.UtcNow,
                Enumerable.Empty<EjercicioRutina>());

            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: Guid.NewGuid(), // different session
                usuarioId: sesion.UsuarioId,
                adaptationProposalId: "prop-123",
                proposalHash: "hash-abc",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: 45,
                fechaConfirmacionUtc: DateTime.UtcNow);

            Assert.Throws<InvalidOperationException>(() => sesion.AsignarAdaptacion(adaptacion));
        }

        [Fact]
        public void SesionEntrenamiento_AsignarAdaptacion_MismatchedUsuarioId_ThrowsInvalidOperationException()
        {
            var sesion = new SesionEntrenamiento(
                "user-1",
                Guid.NewGuid(),
                DateTime.UtcNow,
                Enumerable.Empty<EjercicioRutina>());

            var adaptacion = new SesionAdaptacion(
                sesionEntrenamientoId: sesion.Id,
                usuarioId: "other-user", // different user
                adaptationProposalId: "prop-123",
                proposalHash: "hash-abc",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: 45,
                fechaConfirmacionUtc: DateTime.UtcNow);

            var ex = Assert.Throws<InvalidOperationException>(() => sesion.AsignarAdaptacion(adaptacion));
            Assert.Contains("usuario diferente", ex.Message);
        }

        [Fact]
        public void SesionEntrenamiento_AsignarAdaptacion_AlreadyAssignedDifferentAdaptacion_ThrowsInvalidOperationException()
        {
            var sesion = new SesionEntrenamiento(
                "user-1",
                Guid.NewGuid(),
                DateTime.UtcNow,
                Enumerable.Empty<EjercicioRutina>());

            var adaptacion1 = new SesionAdaptacion(
                sesionEntrenamientoId: sesion.Id,
                usuarioId: sesion.UsuarioId,
                adaptationProposalId: "prop-1",
                proposalHash: "hash-1",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: 45,
                fechaConfirmacionUtc: DateTime.UtcNow);

            var adaptacion2 = new SesionAdaptacion(
                sesionEntrenamientoId: sesion.Id,
                usuarioId: sesion.UsuarioId,
                adaptationProposalId: "prop-2",
                proposalHash: "hash-2",
                policyVersion: "V3",
                factibilidad: FactibilidadAdaptacionSesion.FactibleConCambios,
                effortPreference: PreferenciaEsfuerzoSesion.Normal,
                availableMinutes: 45,
                fechaConfirmacionUtc: DateTime.UtcNow);

            sesion.AsignarAdaptacion(adaptacion1);

            Assert.Throws<InvalidOperationException>(() => sesion.AsignarAdaptacion(adaptacion2));
        }
    }
}
