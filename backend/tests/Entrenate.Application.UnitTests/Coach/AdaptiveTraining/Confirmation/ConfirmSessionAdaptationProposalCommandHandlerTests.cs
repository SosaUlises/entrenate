using Entrenate.Application.Coach.AdaptiveTraining.Commands.ConfirmSessionAdaptationProposal;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Confirmation
{
    public sealed class ConfirmSessionAdaptationProposalCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidRequest_ConfirmsAndCreatesSessionAndAdaptation()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var confirmHandler = fixture.CreateConfirmHandler();

            // Generate proposal with only Dumbbell available (BenchPress requires Barbell, so it substitutes to DumbbellBenchPress)
            // and Lighter mode (RIR increases +1)
            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: new[] { AdaptiveTestFixture.DumbbellId },
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Lighter);

            var command = new ConfirmSessionAdaptationProposalCommand(
                ProposalId: proposal.AdaptationProposalId.ToString(),
                ProposalHash: proposal.ProposalHash,
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: new[] { AdaptiveTestFixture.DumbbellId },
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Lighter);

            var result = await confirmHandler.Handle(command, CancellationToken.None);

            Assert.True(result.CreatedNow);
            Assert.Equal(proposal.AdaptationProposalId.ToString(), result.ProposalId);
            Assert.NotEqual(Guid.Empty, result.SessionId);

            // Verify persisted session in repository
            var session = Assert.Single(fixture.SessionRepository.AddedSessions);
            Assert.Equal(result.SessionId, session.Id);
            Assert.Equal(AdaptiveTestFixture.UserId, session.UsuarioId);
            Assert.Equal(day.Id, session.DiaRutinaId);
            Assert.Equal(EstadoSesionEntrenamiento.EnCurso, session.Estado);

            // Exercise 1: Substituted from BenchPress to DumbbellBenchPress
            var firstExercise = session.Ejercicios.OrderBy(e => e.Orden).First();
            Assert.Equal(fixture.DumbbellBenchPress.Id, firstExercise.EjercicioId);
            Assert.NotEqual(fixture.BenchPress.Id, firstExercise.EjercicioId);
            // Lighter mode: RIR increased from 1-2 to 2-3
            Assert.Equal(2, firstExercise.RirObjetivoMinimo);
            Assert.Equal(3, firstExercise.RirObjetivoMaximo);

            // Base routine was NOT modified
            var baseFirstExercise = day.Ejercicios.OrderBy(e => e.Orden).First();
            Assert.Equal(fixture.BenchPress.Id, baseFirstExercise.EjercicioId);
            Assert.Equal(1, baseFirstExercise.RirObjetivoMinimo);
            Assert.Equal(2, baseFirstExercise.RirObjetivoMaximo);

            // Verify persisted audit adaptation
            var adaptation = Assert.Single(fixture.SessionRepository.AddedAdaptations);
            Assert.Same(session.Adaptacion, adaptation);
            Assert.Equal(session.Id, adaptation.SesionEntrenamientoId);
            Assert.Equal(proposal.AdaptationProposalId.ToString(), adaptation.AdaptationProposalId);
            Assert.Equal(proposal.ProposalHash, adaptation.ProposalHash);
            Assert.Equal(SessionAdaptationProposalEngine.PolicyVersion, adaptation.PolicyVersion);
            Assert.Equal(FactibilidadAdaptacionSesion.FactibleConCambios, adaptation.Factibilidad);
            Assert.Equal(PreferenciaEsfuerzoSesion.Liviana, adaptation.EffortPreference);
            Assert.Equal(45, adaptation.AvailableMinutes);

            // Check deltas
            Assert.Contains(adaptation.Cambios, c =>
                c.TipoCambio == TipoCambioAdaptacionSesion.EjercicioSustituido &&
                c.EjercicioOriginalId == fixture.BenchPress.Id &&
                c.EjercicioAdaptadoId == fixture.DumbbellBenchPress.Id);

            Assert.Contains(adaptation.Cambios, c =>
                c.TipoCambio == TipoCambioAdaptacionSesion.RirAumentado);
        }

        [Fact]
        public async Task Handle_StaleProposalHash_ThrowsConflictException()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var confirmHandler = fixture.CreateConfirmHandler();

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new ConfirmSessionAdaptationProposalCommand(
                ProposalId: proposal.AdaptationProposalId.ToString(),
                ProposalHash: "outdated-hash-12345", // stale hash
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                confirmHandler.Handle(command, CancellationToken.None));

            Assert.Contains("regenerarse", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(fixture.SessionRepository.AddedSessions);
        }

        [Fact]
        public async Task Handle_NotFeasibleProposal_ThrowsConflictException()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDayWithMultipleCompounds();
            var confirmHandler = fixture.CreateConfirmHandler();

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 15, // strictly impossible
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new ConfirmSessionAdaptationProposalCommand(
                ProposalId: proposal.AdaptationProposalId.ToString(),
                ProposalHash: proposal.ProposalHash,
                RoutineDayId: day.Id,
                AvailableMinutes: 15,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                confirmHandler.Handle(command, CancellationToken.None));

            Assert.Contains("no es factible", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(fixture.SessionRepository.AddedSessions);
        }

        [Fact]
        public async Task Handle_IdempotentConfirmation_SameProposalAndHash_ReturnsExistingSession()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var confirmHandler = fixture.CreateConfirmHandler();

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new ConfirmSessionAdaptationProposalCommand(
                ProposalId: proposal.AdaptationProposalId.ToString(),
                ProposalHash: proposal.ProposalHash,
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            // First confirmation
            var firstResult = await confirmHandler.Handle(command, CancellationToken.None);
            Assert.True(firstResult.CreatedNow);

            // An active session is now in progress for the user
            var activeSession = Assert.Single(fixture.SessionRepository.AddedSessions);
            Assert.Equal(EstadoSesionEntrenamiento.EnCurso, activeSession.Estado);

            // Second confirmation (simulating concurrent retry or duplicate request)
            var secondResult = await confirmHandler.Handle(command, CancellationToken.None);
            Assert.False(secondResult.CreatedNow);
            Assert.Equal(firstResult.SessionId, secondResult.SessionId);
            Assert.Equal(firstResult.ProposalId, secondResult.ProposalId);

            // Exactly one session was created in the repository
            Assert.Single(fixture.SessionRepository.AddedSessions);
        }

        [Fact]
        public async Task Handle_IdempotentConfirmation_EvenWhenActiveSessionFlagIsSet_ReturnsExistingSession()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var confirmHandler = fixture.CreateConfirmHandler();

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new ConfirmSessionAdaptationProposalCommand(
                ProposalId: proposal.AdaptationProposalId.ToString(),
                ProposalHash: proposal.ProposalHash,
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            // First confirmation
            var firstResult = await confirmHandler.Handle(command, CancellationToken.None);
            Assert.True(firstResult.CreatedNow);

            // Active session flag is explicitly true
            fixture.SessionRepository.HasActiveSessionValue = true;

            // Second confirmation of the exact same proposal must succeed idempotently
            var secondResult = await confirmHandler.Handle(command, CancellationToken.None);
            Assert.False(secondResult.CreatedNow);
            Assert.Equal(firstResult.SessionId, secondResult.SessionId);
        }

        [Fact]
        public async Task Handle_ActiveSessionInProgress_ThrowsConflictException()
        {
            var fixture = new AdaptiveTestFixture();
            var day = fixture.SetupRoutineDay();
            var confirmHandler = fixture.CreateConfirmHandler();

            fixture.SessionRepository.HasActiveSessionValue = true; // User already has a session in progress

            var proposal = await fixture.Generator.GenerateForUserAsync(
                AdaptiveTestFixture.UserId,
                day.Id,
                availableMinutes: 45,
                availableEquipmentIds: null,
                excludedExerciseIds: null,
                effortPreference: SessionEffortPreference.Normal);

            var command = new ConfirmSessionAdaptationProposalCommand(
                ProposalId: proposal.AdaptationProposalId.ToString(),
                ProposalHash: proposal.ProposalHash,
                RoutineDayId: day.Id,
                AvailableMinutes: 45,
                AvailableEquipmentIds: null,
                ExcludedExerciseIds: null,
                EffortPreference: SessionEffortPreference.Normal);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                confirmHandler.Handle(command, CancellationToken.None));

            Assert.Contains("en curso", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}

