using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Substitution
{
    public class AdaptiveTrainingSubstitutionEngineTests
    {
        [Fact]
        public void Adapt_WhenExerciseIsRealizable_RemainsUnchanged()
        {
            var exercise = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var session = CreateSession(exercise);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [],
                availableEquipmentIds: [Equip(1)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.Feasible,
                result.Feasibility);
            Assert.Empty(result.Changes);
            Assert.Empty(result.Diagnostics);
            Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(
                result.OriginalSession.Exercises[0].ExerciseId,
                result.AdaptedSession.Exercises[0].ExerciseId);
        }

        [Fact]
        public void Adapt_WhenEquipmentUnavailable_SubstitutesWithValidCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]); // Barbell

            var candidate = CreateCandidate(
                id: 2,
                code: "db_bench",
                name: "Press con mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]); // Dumbbell

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.FeasibleWithChanges,
                result.Feasibility);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.Substituted,
                change.Type);
            Assert.Equal(
                AdaptiveSubstitutionReason.EquipmentUnavailable,
                change.Reason);
            Assert.Equal(Id(1), change.OriginalExercise.ExerciseId);
            Assert.NotNull(change.AdaptedExercise);
            Assert.Equal(Id(2), change.AdaptedExercise.ExerciseId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(
                AdaptiveTrainingSubstitutionDiagnosticCode.ExerciseSubstituted,
                diagnostic.Code);
            Assert.Equal(Id(1), diagnostic.ExerciseId);
            Assert.Equal(Id(2), diagnostic.SubstitutedExerciseId);

            var adaptedExercise = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(Id(2), adaptedExercise.ExerciseId);
            Assert.Equal("db_bench", adaptedExercise.ExerciseCode);
        }

        [Fact]
        public void Adapt_WhenExerciseExcluded_SubstitutesWithValidCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "pushup",
                name: "Flexiones de pecho",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: []);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(1)],
                excludedExerciseIds: [Id(1)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.FeasibleWithChanges,
                result.Feasibility);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.Substituted,
                change.Type);
            Assert.Equal(
                AdaptiveSubstitutionReason.UserExcludedExercise,
                change.Reason);
            Assert.Equal(Id(1), change.OriginalExercise.ExerciseId);
            Assert.NotNull(change.AdaptedExercise);
            Assert.Equal(Id(2), change.AdaptedExercise.ExerciseId);
        }

        [Fact]
        public void Adapt_WhenCandidateHasDifferentMovementPattern_RejectsCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "row",
                name: "Remo",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.TraccionHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);
            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.RequiredUnavailable,
                change.Type);
        }

        [Fact]
        public void Adapt_WhenCandidateHasDifferentPrimaryMuscle_RejectsCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "military_press",
                name: "Press militar",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Hombros",
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);
        }

        [Fact]
        public void Adapt_WhenCandidateHasDifferentExerciseType_RejectsCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "flyes",
                name: "Aperturas con mancuernas",
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);
        }

        [Fact]
        public void Adapt_WhenCandidateRequiresUnavailableEquipment_RejectsCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "cable_press",
                name: "Press en polea",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(3)]); // Polea not available

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)]); // Only dumbbell available

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);
        }

        [Fact]
        public void Adapt_WhenCandidateIsExcluded_RejectsCandidate()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "db_bench",
                name: "Press con mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)],
                excludedExerciseIds: [Id(2)]); // Candidate excluded

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);
        }

        [Fact]
        public void Adapt_WhenCandidateAlreadyPresentInSession_RejectsCandidate()
        {
            var exercise1 = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]); // Needs barbell, missing

            var exercise2 = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]); // Dumbbell bench, already in session and realizable

            var candidateSameAsEx2 = CreateCandidate(
                id: 2,
                code: "db_bench",
                name: "Press con mancuernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]);

            var session = CreateSession(exercise1, exercise2);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidateSameAsEx2],
                availableEquipmentIds: [Equip(2)]);

            // Candidate 2 is already present in session, cannot be reused as substitute for exercise 1
            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);
            var change = Assert.Single(result.Changes);
            Assert.Equal(Id(1), change.OriginalExercise.ExerciseId);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.RequiredUnavailable,
                change.Type);
        }

        [Fact]
        public void Adapt_RanksCandidatesBySharedSecondaryMuscles()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                secondaries: ["Tríceps", "Deltoides anterior"],
                requiredEquip: [Equip(1)]);

            // Candidate A matches 2 secondary muscles, but has higher alphabetical code
            var candidateA = CreateCandidate(
                id: 10,
                code: "zzz_press",
                name: "Press Z",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                secondaries: ["Tríceps", "Deltoides anterior"],
                requiredEquip: [Equip(2)]);

            // Candidate B matches only 1 secondary muscle, but has lower alphabetical code
            var candidateB = CreateCandidate(
                id: 11,
                code: "aaa_press",
                name: "Press A",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                secondaries: ["Tríceps"],
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidateB, candidateA],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.FeasibleWithChanges,
                result.Feasibility);
            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.Substituted,
                change.Type);
            Assert.NotNull(change.AdaptedExercise);
            Assert.Equal(Id(10), change.AdaptedExercise.ExerciseId);
            Assert.Equal("zzz_press", change.AdaptedExercise.ExerciseCode);
        }

        [Fact]
        public void Adapt_WhenRequiredExerciseHasNoSubstitute_ReturnsNotFeasibleAndKeepsExercise()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.DominanteRodilla,
                primaryMuscle: "Cuádriceps",
                requiredEquip: [Equip(1)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.NotFeasible,
                result.Feasibility);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.RequiredUnavailable,
                change.Type);
            Assert.Equal(Id(1), change.OriginalExercise.ExerciseId);
            Assert.NotNull(change.AdaptedExercise);
            Assert.Equal(Id(1), change.AdaptedExercise.ExerciseId);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(
                AdaptiveTrainingSubstitutionDiagnosticCode.RequiredExerciseHasNoSubstitute,
                diagnostic.Code);
            Assert.Equal(Id(1), diagnostic.ExerciseId);

            // Exercise was NOT eliminated from the adapted session
            var adaptedExercise = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(Id(1), adaptedExercise.ExerciseId);
        }

        [Fact]
        public void Adapt_WhenOptionalAccessoryHasNoSubstitute_RemovesExerciseAndSessionRemainsFeasible()
        {
            var requiredCompound = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(2)]); // Dumbbell, realizable

            var optionalAccessory = CreateExercise(
                id: 2,
                order: 2,
                type: TipoEjercicio.Aislamiento,
                pattern: PatronMovimiento.AbduccionHombro,
                primaryMuscle: "Hombros",
                requiredEquip: [Equip(3)], // Cable, missing
                isRequiredIntent: false);

            var session = CreateSession(requiredCompound, optionalAccessory);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.FeasibleWithChanges,
                result.Feasibility);

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveTrainingSubstitutionChangeType.OptionalRemoved,
                change.Type);
            Assert.Equal(Id(2), change.OriginalExercise.ExerciseId);
            Assert.Null(change.AdaptedExercise);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(
                AdaptiveTrainingSubstitutionDiagnosticCode
                    .OptionalExerciseRemovedNoSubstitute,
                diagnostic.Code);
            Assert.Equal(Id(2), diagnostic.ExerciseId);

            // Optional accessory was eliminated from adapted session
            var adaptedExercise = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(Id(1), adaptedExercise.ExerciseId);
        }

        [Fact]
        public void Adapt_PreservesOriginalPrescription()
        {
            var customPrescription = new AdaptiveTrainingPrescription(
                Sets: 4,
                MinReps: 6,
                MaxReps: 10,
                MinRir: 1,
                MaxRir: 2,
                RestSeconds: 150,
                Notes: "Última serie al fallo");

            var original = CreateExercise(
                id: 1,
                order: 3,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.DominanteRodilla,
                primaryMuscle: "Cuádriceps",
                requiredEquip: [Equip(1)],
                prescription: customPrescription);

            var candidate = CreateCandidate(
                id: 2,
                code: "leg_press",
                name: "Prensa de piernas",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.DominanteRodilla,
                primaryMuscle: "Cuádriceps",
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [Equip(2)]);

            var adaptedExercise = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(3, adaptedExercise.Order);
            Assert.Equal(Id(2), adaptedExercise.ExerciseId);
            Assert.Equal(4, adaptedExercise.Prescription.Sets);
            Assert.Equal(6, adaptedExercise.Prescription.MinReps);
            Assert.Equal(10, adaptedExercise.Prescription.MaxReps);
            Assert.Equal(1, adaptedExercise.Prescription.MinRir);
            Assert.Equal(2, adaptedExercise.Prescription.MaxRir);
            Assert.Equal(150, adaptedExercise.Prescription.RestSeconds);
            Assert.Equal("Última serie al fallo", adaptedExercise.Prescription.Notes);
        }

        [Fact]
        public void Adapt_ProducesIdenticalResultRegardlessOfCatalogOrder()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                secondaries: ["Tríceps"],
                requiredEquip: [Equip(1)]);

            var candidate1 = CreateCandidate(
                id: 2,
                code: "db_bench_flat",
                name: "Press plano",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                secondaries: ["Tríceps"],
                requiredEquip: [Equip(2)]);

            var candidate2 = CreateCandidate(
                id: 3,
                code: "db_bench_incline",
                name: "Press inclinado",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                secondaries: ["Tríceps"],
                requiredEquip: [Equip(2)]);

            var session = CreateSession(original);

            var resultA = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate1, candidate2],
                availableEquipmentIds: [Equip(2)]);

            var resultB = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate2, candidate1],
                availableEquipmentIds: [Equip(2)]);

            Assert.Equal(
                resultA.AdaptedSession.Exercises[0].ExerciseId,
                resultB.AdaptedSession.Exercises[0].ExerciseId);
            Assert.Equal(
                resultA.AdaptedSession.Exercises[0].ExerciseCode,
                resultB.AdaptedSession.Exercises[0].ExerciseCode);
            Assert.Equal(
                resultA.Changes[0].AdaptedExercise!.ExerciseId,
                resultB.Changes[0].AdaptedExercise!.ExerciseId);
        }

        [Fact]
        public void Adapt_WhenBothExcludedAndMissingEquipment_CombinesReason()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var candidate = CreateCandidate(
                id: 2,
                code: "pushup",
                name: "Flexiones",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: []);

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [candidate],
                availableEquipmentIds: [], // Missing equip
                excludedExerciseIds: [Id(1)]); // Also excluded

            var change = Assert.Single(result.Changes);
            Assert.Equal(
                AdaptiveSubstitutionReason.EquipmentUnavailable |
                AdaptiveSubstitutionReason.UserExcludedExercise,
                change.Reason);
        }

        [Fact]
        public void Adapt_BodyweightCandidate_IsCompatibleWithNoEquipment()
        {
            var original = CreateExercise(
                id: 1,
                order: 1,
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: [Equip(1)]);

            var bodyweightCandidate = CreateCandidate(
                id: 2,
                code: "bodyweight_pushup",
                name: "Flexiones",
                type: TipoEjercicio.Compuesto,
                pattern: PatronMovimiento.EmpujeHorizontal,
                primaryMuscle: "Pecho",
                requiredEquip: []); // Empty = bodyweight

            var session = CreateSession(original);

            var result = AdaptiveTrainingSubstitutionEngine.Adapt(
                session,
                catalog: [bodyweightCandidate],
                availableEquipmentIds: []); // No equipment available today

            Assert.Equal(
                AdaptiveTrainingSubstitutionFeasibility.FeasibleWithChanges,
                result.Feasibility);
            var adapted = Assert.Single(result.AdaptedSession.Exercises);
            Assert.Equal(Id(2), adapted.ExerciseId);
        }

        private static PlannedSubstitutionSession CreateSession(
            params PlannedSubstitutionExercise[] exercises)
        {
            return new PlannedSubstitutionSession(
                Guid.NewGuid(),
                exercises);
        }

        private static PlannedSubstitutionExercise CreateExercise(
            int id,
            int order,
            TipoEjercicio type,
            PatronMovimiento pattern,
            string primaryMuscle,
            IEnumerable<string>? secondaries = null,
            IEnumerable<Guid>? requiredEquip = null,
            bool? isRequiredIntent = null,
            AdaptiveTrainingPrescription? prescription = null,
            string? code = null)
        {
            return new PlannedSubstitutionExercise(
                Guid.NewGuid(),
                Id(id),
                code ?? $"ex_{id}",
                $"Exercise {id}",
                order,
                pattern,
                type,
                primaryMuscle,
                secondaries?.ToArray() ?? [],
                requiredEquip?.ToArray() ?? [],
                isRequiredIntent,
                prescription ?? new AdaptiveTrainingPrescription(
                    Sets: 3,
                    MinReps: 8,
                    MaxReps: 12,
                    MinRir: 1,
                    MaxRir: 3,
                    RestSeconds: 90,
                    Notes: null));
        }

        private static AdaptiveSubstitutionCandidate CreateCandidate(
            int id,
            string code,
            string name,
            TipoEjercicio type,
            PatronMovimiento pattern,
            string primaryMuscle,
            IEnumerable<string>? secondaries = null,
            IEnumerable<Guid>? requiredEquip = null,
            bool isActive = true)
        {
            return new AdaptiveSubstitutionCandidate(
                Id(id),
                code,
                name,
                pattern,
                type,
                primaryMuscle,
                secondaries?.ToArray() ?? [],
                isActive,
                requiredEquip?.ToArray() ?? []);
        }

        private static Guid Id(int value) =>
            new($"00000000-0000-0000-0000-{value:D12}");

        private static Guid Equip(int value) =>
            new($"00000000-0000-0000-0001-{value:D12}");
    }
}

