using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Proposal
{
    public class SessionAdaptationProposalEngineTests
    {
        [Fact]
        public void Adapt_WithoutRestrictions_ReturnsFeasibleWithoutChanges()
        {
            var routineDayId = Guid.NewGuid();
            var exercise = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho");

            var session = new PlannedAdaptiveSession(routineDayId, [exercise]);
            var request = new SessionAdaptationRequest(routineDayId);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);

            Assert.Equal(SessionAdaptationFeasibility.Feasible, proposal.Feasibility);
            Assert.Empty(proposal.Changes);
            Assert.Empty(proposal.Diagnostics);
            Assert.Single(proposal.AdaptedSession.Exercises);
            Assert.Equal(
                proposal.OriginalSession.EstimatedDurationSeconds,
                proposal.AdaptedSession.EstimatedDurationSeconds);
            Assert.False(string.IsNullOrWhiteSpace(proposal.ProposalHash));
            Assert.NotEqual(Guid.Empty, proposal.AdaptationProposalId);
        }

        [Fact]
        public void Adapt_TimeOnly_ReflectsTimeAdaptation()
        {
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                restSeconds: 120);

            var optionalAccessory = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.ExtensionCodo,
                primaryMuscle: "Tríceps",
                sets: 4,
                restSeconds: 90,
                isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(
                routineDayId,
                [compound, optionalAccessory]);

            // Session duration exceeds 16 minutes, so 15 min available requires reduction
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 15);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);

            Assert.Equal(
                SessionAdaptationFeasibility.FeasibleWithChanges,
                proposal.Feasibility);
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.ExerciseSetsReducedForTime ||
                     c.ChangeType == SessionAdaptationChangeType.OptionalAccessoryRemovedForTime);
            Assert.True(
                proposal.AdaptedSession.EstimatedDurationSeconds <= 15 * 60);
        }

        [Fact]
        public void Adapt_EquipmentOnly_ReflectsSubstitution()
        {
            var routineDayId = Guid.NewGuid();
            var barbellBench = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]); // Barbell

            var session = new PlannedAdaptiveSession(routineDayId, [barbellBench]);

            var candidateDbBench = CreateCandidate(
                id: 2,
                code: "db_bench",
                name: "Press con mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]); // Dumbbell

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(2)]); // Barbell missing

            var proposal = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: [candidateDbBench]);

            Assert.Equal(
                SessionAdaptationFeasibility.FeasibleWithChanges,
                proposal.Feasibility);

            var change = Assert.Single(proposal.Changes);
            Assert.Equal(
                SessionAdaptationChangeType.ExerciseSubstituted,
                change.ChangeType);
            Assert.Equal(Id(1), change.ExerciseId);
            Assert.Equal(Id(2), change.SubstitutedExerciseId);

            var adaptedExercise = Assert.Single(proposal.AdaptedSession.Exercises);
            Assert.Equal(Id(2), adaptedExercise.ExerciseId);
            Assert.Equal("db_bench", adaptedExercise.ExerciseCode);
        }

        [Fact]
        public void Adapt_LighterOnly_ReflectsLighterMode()
        {
            var routineDayId = Guid.NewGuid();
            var exercise = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                minRir: 1,
                maxRir: 2);

            var session = new PlannedAdaptiveSession(routineDayId, [exercise]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);

            Assert.Equal(
                SessionAdaptationFeasibility.FeasibleWithChanges,
                proposal.Feasibility);

            var change = Assert.Single(proposal.Changes);
            Assert.Equal(
                SessionAdaptationChangeType.RirIncreased,
                change.ChangeType);
            Assert.Equal(1, change.OriginalMinRir);
            Assert.Equal(2, change.AdaptedMinRir);
            Assert.Equal(2, change.OriginalMaxRir);
            Assert.Equal(3, change.AdaptedMaxRir);

            var adapted = Assert.Single(proposal.AdaptedSession.Exercises);
            Assert.Equal(2, adapted.Prescription.MinRir);
            Assert.Equal(3, adapted.Prescription.MaxRir);
        }

        [Fact]
        public void Adapt_SubstitutionPlusTime_ExecutesInOrder()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)], // Barbell missing
                sets: 3,
                restSeconds: 120);

            var ex2 = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.ExtensionCodo,
                primaryMuscle: "Tríceps",
                requiredEquip: [Equip(2)], // Dumbbell available
                sets: 4,
                restSeconds: 90,
                isRequiredIntent: false);

            var candidate1 = CreateCandidate(
                id: 10,
                code: "db_bench",
                name: "Press mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1, ex2]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 15,
                AvailableEquipmentIds: [Equip(2)]);

            var proposal = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: [candidate1]);

            Assert.Equal(
                SessionAdaptationFeasibility.FeasibleWithChanges,
                proposal.Feasibility);

            // Substituted in Stage 1
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.ExerciseSubstituted);

            // Reduced/removed for time in Stage 2
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.ExerciseSetsReducedForTime ||
                     c.ChangeType == SessionAdaptationChangeType.OptionalAccessoryRemovedForTime);
        }

        [Fact]
        public void Adapt_TimePlusLighter_ExecutesInOrder()
        {
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                minRir: 1,
                maxRir: 2,
                restSeconds: 120);

            var accessory = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.FlexionCodo,
                primaryMuscle: "Bíceps",
                sets: 4,
                minRir: 2,
                maxRir: 3,
                restSeconds: 90,
                isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 18,
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);

            Assert.Equal(
                SessionAdaptationFeasibility.FeasibleWithChanges,
                proposal.Feasibility);

            // Time changes
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.ExerciseSetsReducedForTime ||
                     c.ChangeType == SessionAdaptationChangeType.OptionalAccessoryRemovedForTime);

            // Lighter changes
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.RirIncreased);
        }

        [Fact]
        public void Adapt_SubstitutionPlusTimePlusLighter_ExecutesAllStages()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)], // Missing barbell
                sets: 3,
                minRir: 1,
                maxRir: 2,
                restSeconds: 120);

            var ex2 = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.FlexionCodo,
                primaryMuscle: "Bíceps",
                requiredEquip: [Equip(2)], // Dumbbell
                sets: 4,
                minRir: 1,
                maxRir: 2,
                restSeconds: 90,
                isRequiredIntent: false);

            var candidate1 = CreateCandidate(
                id: 10,
                code: "db_bench",
                name: "Press mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1, ex2]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 18,
                AvailableEquipmentIds: [Equip(2)],
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: [candidate1]);

            Assert.Equal(
                SessionAdaptationFeasibility.FeasibleWithChanges,
                proposal.Feasibility);

            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.ExerciseSubstituted);
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.RirIncreased);
        }

        [Fact]
        public void Adapt_WhenSubstitutionNotFeasible_PropagatesNotFeasible()
        {
            var routineDayId = Guid.NewGuid();
            var requiredCompound = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.DominanteRodilla,
                primaryMuscle: "Cuádriceps",
                requiredEquip: [Equip(1)]); // Missing barbell

            var session = new PlannedAdaptiveSession(routineDayId, [requiredCompound]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(2)]); // Dumbbell only, no candidate

            var proposal = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: []);

            Assert.Equal(
                SessionAdaptationFeasibility.NotFeasible,
                proposal.Feasibility);
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.RequiredExerciseUnavailable);
            Assert.Contains(
                proposal.Diagnostics,
                d => d.Code == SessionAdaptationDiagnosticCode.RequiredExerciseHasNoSubstitute);
            Assert.Single(proposal.AdaptedSession.Exercises); // Not eliminated
        }

        [Fact]
        public void Adapt_WhenTimeNotFeasible_PropagatesNotFeasible()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                sets: 3,
                restSeconds: 180);

            var ex2 = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.TraccionHorizontal,
                primaryMuscle: "Espalda",
                sets: 3,
                restSeconds: 180);

            var ex3 = CreateExercise(
                id: 3,
                order: 3,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.DominanteRodilla,
                primaryMuscle: "Piernas",
                sets: 3,
                restSeconds: 180);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1, ex2, ex3]);

            // Minimum required compound sets is 2 each (270s each + 120s transitions + 300s prep = 1230s > 900s).
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 15);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);

            Assert.Equal(
                SessionAdaptationFeasibility.NotFeasible,
                proposal.Feasibility);
            Assert.Contains(
                proposal.Diagnostics,
                d => d.Code == SessionAdaptationDiagnosticCode.MinimumSessionExceedsAvailableTime);
        }

        [Fact]
        public void Adapt_ConsolidatesChangesAndDiagnosticsCorrectly()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)],
                sets: 3);

            var candidate1 = CreateCandidate(
                id: 10,
                code: "db_bench",
                name: "Press mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(2)],
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: [candidate1]);

            // Ex1 was substituted AND lighter mode increased RIR
            Assert.Equal(2, proposal.Changes.Count);
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.ExerciseSubstituted);
            Assert.Contains(
                proposal.Changes,
                c => c.ChangeType == SessionAdaptationChangeType.RirIncreased);

            Assert.Equal(2, proposal.Diagnostics.Count);
            Assert.Contains(
                proposal.Diagnostics,
                d => d.Code == SessionAdaptationDiagnosticCode.ExerciseSubstituted);
            Assert.Contains(
                proposal.Diagnostics,
                d => d.Code == SessionAdaptationDiagnosticCode.RirIncreased);
        }

        [Fact]
        public void Adapt_ProducesIdenticalHashAndProposalId_ForSameInput()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)],
                sets: 3);

            var candidate1 = CreateCandidate(
                id: 10,
                code: "db_bench",
                name: "Press mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1]);

            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 20,
                AvailableEquipmentIds: [Equip(2)],
                EffortPreference: SessionEffortPreference.Lighter);

            var proposalA = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: [candidate1]);

            var proposalB = SessionAdaptationProposalEngine.Adapt(
                session,
                request,
                catalog: [candidate1]);

            Assert.Equal(proposalA.ProposalHash, proposalB.ProposalHash);
            Assert.Equal(proposalA.AdaptationProposalId, proposalB.AdaptationProposalId);
        }

        [Fact]
        public void Adapt_ProducesIdenticalHashAndProposalId_WhenIncidentalOrderDiffers()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)],
                sets: 3);

            var candidateA = CreateCandidate(
                id: 10,
                code: "db_bench",
                name: "Press mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var candidateB = CreateCandidate(
                id: 11,
                code: "pushup",
                name: "Flexiones",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: []);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1]);

            var request1 = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(2), Equip(3)],
                ExcludedExerciseIds: [Id(50), Id(51)]);

            var request2 = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(3), Equip(2)], // inverted equipment order
                ExcludedExerciseIds: [Id(51), Id(50)]); // inverted exclusion order

            var proposal1 = SessionAdaptationProposalEngine.Adapt(
                session,
                request1,
                catalog: [candidateA, candidateB]);

            var proposal2 = SessionAdaptationProposalEngine.Adapt(
                session,
                request2,
                catalog: [candidateB, candidateA]); // inverted catalog order

            Assert.Equal(proposal1.ProposalHash, proposal2.ProposalHash);
            Assert.Equal(proposal1.AdaptationProposalId, proposal2.AdaptationProposalId);
        }

        [Fact]
        public void Adapt_ProducesDifferentHash_WhenRelevantContextChanges()
        {
            var routineDayId = Guid.NewGuid();
            var ex1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)],
                sets: 3);

            var session = new PlannedAdaptiveSession(routineDayId, [ex1]);

            var baseRequest = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 20,
                AvailableEquipmentIds: [Equip(1)]);

            var requestDifferentMinutes = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 30, // changed
                AvailableEquipmentIds: [Equip(1)]);

            var requestDifferentEffort = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 20,
                AvailableEquipmentIds: [Equip(1)],
                EffortPreference: SessionEffortPreference.Lighter); // changed

            var proposalBase = SessionAdaptationProposalEngine.Adapt(session, baseRequest);
            var proposalDiffMinutes = SessionAdaptationProposalEngine.Adapt(session, requestDifferentMinutes);
            var proposalDiffEffort = SessionAdaptationProposalEngine.Adapt(session, requestDifferentEffort);

            Assert.NotEqual(proposalBase.ProposalHash, proposalDiffMinutes.ProposalHash);
            Assert.NotEqual(proposalBase.AdaptationProposalId, proposalDiffMinutes.AdaptationProposalId);

            Assert.NotEqual(proposalBase.ProposalHash, proposalDiffEffort.ProposalHash);
            Assert.NotEqual(proposalBase.AdaptationProposalId, proposalDiffEffort.AdaptationProposalId);
        }

        private static PlannedAdaptiveExercise CreateExercise(
            int id,
            int order,
            TipoEjercicio? type,
            PatronMovimiento? pattern,
            string primaryMuscle,
            int sets = 3,
            int minReps = 8,
            int maxReps = 12,
            int minRir = 1,
            int maxRir = 2,
            int restSeconds = 90,
            bool? isRequiredIntent = null,
            IEnumerable<Guid>? requiredEquip = null)
        {
            return new PlannedAdaptiveExercise(
                Guid.NewGuid(),
                Id(id),
                $"ex_{id}",
                $"Exercise {id}",
                order,
                pattern,
                type,
                primaryMuscle,
                ["Secundario"],
                requiredEquip?.ToArray() ?? [],
                isRequiredIntent,
                new AdaptiveTrainingPrescription(
                    Sets: sets,
                    MinReps: minReps,
                    MaxReps: maxReps,
                    MinRir: minRir,
                    MaxRir: maxRir,
                    RestSeconds: restSeconds,
                    Notes: null));
        }

        private static AdaptiveSubstitutionCandidate CreateCandidate(
            int id,
            string code,
            string name,
            TipoEjercicio type,
            PatronMovimiento pattern,
            string primaryMuscle,
            IEnumerable<Guid>? requiredEquip = null)
        {
            return new AdaptiveSubstitutionCandidate(
                Id(id),
                code,
                name,
                pattern,
                type,
                primaryMuscle,
                ["Secundario"],
                true,
                requiredEquip?.ToArray() ?? []);
        }

        private static Guid Id(int value) =>
            new($"00000000-0000-0000-0000-{value:D12}");

        private static Guid Equip(int value) =>
            new($"00000000-0000-0000-0001-{value:D12}");
    }
}
