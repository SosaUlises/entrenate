using System.Text.Json;
using System.Text.RegularExpressions;
using Entrenate.Application.Coach.AdaptiveTraining.Conversation;
using Entrenate.Application.Coach.AdaptiveTraining.Explanations;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Explanations
{
    public sealed class SessionAdaptationExplanationTests
    {
        [Fact]
        public void Generate_NullProposal_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SessionAdaptationExplanationGenerator.Generate(null!));
        }

        [Fact]
        public void Generate_NoChanges_ProducesClearSummaryAndEmptyChanges()
        {
            // Case 1: propuesta sin cambios
            var routineDayId = Guid.NewGuid();
            var ex = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho");
            var session = new PlannedAdaptiveSession(routineDayId, [ex]);
            var request = new SessionAdaptationRequest(routineDayId);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.NotNull(explanation);
            Assert.Contains("No fue necesario adaptar la sesión", explanation.Summary);
            Assert.Empty(explanation.Changes);
            Assert.Empty(explanation.Warnings);
            Assert.Contains("duración estimada", explanation.Rationale);
        }

        [Fact]
        public void Generate_EquipmentSubstitution_ExplainsOriginalAndSubstituteWithReason()
        {
            // Case 2: sustitución por equipamiento
            var routineDayId = Guid.NewGuid();
            var barbellBench = CreateExercise(
                1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho",
                requiredEquip: [Equip(1)]); // Requires barbell
            var candidateDumbbell = CreateCandidate(
                2, "db_bench", "Dumbbell Bench Press", TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho",
                requiredEquip: [Equip(2)]); // Requires dumbbell

            var session = new PlannedAdaptiveSession(routineDayId, [barbellBench]);
            // Only dumbbell is available
            var request = new SessionAdaptationRequest(routineDayId, AvailableEquipmentIds: [Equip(2)]);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, [candidateDumbbell]);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Single(explanation.Changes);
            var changeText = explanation.Changes[0];
            Assert.Contains("Exercise 1", changeText);
            Assert.Contains("Dumbbell Bench Press", changeText);
            Assert.Contains("equipamiento", changeText);
            Assert.Contains("patrón de movimiento", changeText);
            Assert.Contains("reemplazaron ejercicios", explanation.Summary);
        }

        [Fact]
        public void Generate_ExclusionSubstitution_ExplainsExclusionReasonWithoutMedicalOrFatigueInference()
        {
            // Case 3: sustitución por exclusión del usuario
            var routineDayId = Guid.NewGuid();
            var bench = CreateExercise(
                1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho",
                requiredEquip: [Equip(1)]);
            var subCandidate = CreateCandidate(
                2, "sub_bench", "Substitute Press", TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho",
                requiredEquip: [Equip(1)]);

            var session = new PlannedAdaptiveSession(routineDayId, [bench]);
            // User excludes bench
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(1)],
                ExcludedExerciseIds: [Id(1)]);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, [subCandidate]);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Single(explanation.Changes);
            var changeText = explanation.Changes[0];
            Assert.Contains("decidiste no realizarlo en esta sesión", changeText);
            Assert.Contains("Substitute Press", changeText);

            // Debe garantizar que NO infiere dolor, lesión, molestia, causa médica ni fatiga
            Assert.DoesNotContain("dolor", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lesión", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lesion", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("molestia", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("médic", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("medic", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fatiga", changeText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Generate_CombinedExclusionAndEquipmentSubstitution_ExplainsBothReasonsWithoutMedicalInference()
        {
            var routineDayId = Guid.NewGuid();
            var bench = CreateExercise(
                1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho",
                requiredEquip: [Equip(1)]);
            var subCandidate = CreateCandidate(
                2, "sub_bench", "Substitute Press", TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho",
                requiredEquip: [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [bench]);
            // Equip 1 unavailable AND user excluded bench
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableEquipmentIds: [Equip(2)],
                ExcludedExerciseIds: [Id(1)]);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, [subCandidate]);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Single(explanation.Changes);
            var changeText = explanation.Changes[0];
            Assert.Contains("fue excluido para esta sesión y su equipamiento tampoco está disponible", changeText);

            Assert.DoesNotContain("dolor", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lesión", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("lesion", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("molestia", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("médic", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("medic", changeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fatiga", changeText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Generate_OptionalAccessoryRemovedNoSubstitute_ExplainsRemovalWithoutSubstitute()
        {
            // Case 4: accesorio eliminado sin sustituto
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", requiredEquip: [Equip(1)]);
            var optionalAccessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", isRequiredIntent: false, requiredEquip: [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, optionalAccessory]);
            // Only equip 1 available -> accessory cannot be performed and has no candidate
            var request = new SessionAdaptationRequest(routineDayId, AvailableEquipmentIds: [Equip(1)]);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, []);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains(explanation.Changes, c => c.Contains("retiró el accesorio opcional") && c.Contains("Exercise 2"));
            Assert.Contains(explanation.Warnings, w => w.Contains("retirado") && w.Contains("Exercise 2"));
        }

        [Fact]
        public void Generate_SetsReducedForTime_ExplainsOriginalAndAdaptedSets()
        {
            // Case 5: reducción de series por tiempo
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", sets: 3, restSeconds: 120);
            var accessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", sets: 4, restSeconds: 90, isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);
            // Session exceeds 16 min -> available 15 min reduces sets
            var request = new SessionAdaptationRequest(routineDayId, AvailableMinutes: 15);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains(explanation.Changes, c => c.Contains("redujeron las series") && c.Contains("Exercise 2"));
            Assert.Contains("tiempo disponible", explanation.Summary);
        }

        [Fact]
        public void Generate_OptionalAccessoryRemovedForTime_ExplainsTimeAdjustment()
        {
            // Case 6: accesorio eliminado por tiempo
            var routineDayId = Guid.NewGuid();
            var c1 = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", sets: 4, restSeconds: 180);
            var c2 = CreateExercise(2, 2, TipoEjercicio.Compuesto, PatronMovimiento.DominanteRodilla, "Cuádriceps", sets: 4, restSeconds: 180);
            var accessory = CreateExercise(3, 3, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", sets: 1, restSeconds: 90, isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(routineDayId, [c1, c2, accessory]);
            var request = new SessionAdaptationRequest(routineDayId, AvailableMinutes: 25);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains(explanation.Changes, c => c.Contains("retiró el accesorio opcional") && c.Contains("tiempo disponible"));
        }

        [Fact]
        public void Generate_RirIncreased_ExplainsLighterEffort()
        {
            // Case 7: RIR aumentado
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", minRir: 1, maxRir: 2);
            var session = new PlannedAdaptiveSession(routineDayId, [compound]);
            var request = new SessionAdaptationRequest(routineDayId, EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains(explanation.Changes, c => c.Contains("incrementó el RIR objetivo") && c.Contains("2-3"));
            Assert.Contains("modo más liviano", explanation.Summary);
        }

        [Fact]
        public void Generate_OptionalAccessorySetsReducedForLighter_ExplainsReducedSetsForLighter()
        {
            // Case 8: reducción de accesorio por modo liviano
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", sets: 3);
            var accessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", sets: 3, isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);
            var request = new SessionAdaptationRequest(routineDayId, EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains(explanation.Changes, c => c.Contains("redujo una serie en el accesorio opcional") && c.Contains("Exercise 2"));
            // Compound must NOT have sets reduced
            Assert.DoesNotContain(explanation.Changes, c => c.Contains("redujeron las series de 'Exercise 1'"));
        }

        [Fact]
        public void Generate_MultipleChangesCombined_ProducesCombinedSummaryAndAllChanges()
        {
            // Case 9: combinación de varios cambios
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", requiredEquip: [Equip(1)], sets: 3);
            var accessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", requiredEquip: [Equip(2)], sets: 3, isRequiredIntent: false);
            var candidate = CreateCandidate(3, "db_bench", "Dumbbell Bench", TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);
            // Only equip 2, limited time 18 min, and lighter mode
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 18,
                AvailableEquipmentIds: [Equip(2)],
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, [candidate]);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains("combinaron varias adaptaciones", explanation.Summary);
            Assert.NotEmpty(explanation.Changes);
            // Has substitution
            Assert.Contains(explanation.Changes, c => c.Contains("reemplazó"));
            // Has lighter/RIR
            Assert.Contains(explanation.Changes, c => c.Contains("RIR"));
        }

        [Fact]
        public void Generate_NotFeasible_RequiredNoSubstitute_ExplainsCauseAndWarnings()
        {
            // Case 10: NotFeasible por requerido sin sustituto
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", requiredEquip: [Equip(1)]);
            var session = new PlannedAdaptiveSession(routineDayId, [compound]);
            // Equip 1 unavailable and no candidates
            var request = new SessionAdaptationRequest(routineDayId, AvailableEquipmentIds: [Equip(2)]);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, []);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains("La sesión no puede adaptarse respetando las restricciones y reglas actuales", explanation.Summary);
            Assert.Contains(explanation.Warnings, w => w.Contains("no cuenta con un reemplazo válido con las restricciones actuales"));
        }

        [Fact]
        public void Generate_NotFeasible_InsufficientTime_ExplainsTimeCauseAndWarnings()
        {
            // Case 11: NotFeasible por tiempo
            var routineDayId = Guid.NewGuid();
            var c1 = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", sets: 3, restSeconds: 180);
            var c2 = CreateExercise(2, 2, TipoEjercicio.Compuesto, PatronMovimiento.DominanteRodilla, "Cuádriceps", sets: 3, restSeconds: 180);
            var c3 = CreateExercise(3, 3, TipoEjercicio.Compuesto, PatronMovimiento.TraccionHorizontal, "Espalda", sets: 3, restSeconds: 180);
            var c4 = CreateExercise(4, 4, TipoEjercicio.Compuesto, PatronMovimiento.BisagraCadera, "Isquios", sets: 3, restSeconds: 180);
            var c5 = CreateExercise(5, 5, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeVertical, "Hombros", sets: 3, restSeconds: 180);

            var session = new PlannedAdaptiveSession(routineDayId, [c1, c2, c3, c4, c5]);
            // Minimum time for 5 compounds * 2 sets floor = >35 min; 15 min available cannot fit them
            var request = new SessionAdaptationRequest(routineDayId, AvailableMinutes: 15);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            Assert.Contains("La sesión no puede adaptarse respetando las restricciones y reglas actuales", explanation.Summary);
            Assert.Contains(explanation.Warnings, w => w.Contains("Incluso la versión mínima permitida de la sesión supera el tiempo disponible"));
        }

        [Fact]
        public void Generate_Duration_ExplainsOriginalAndAdaptedDurationsAndAvailableTime()
        {
            // Case 12: duración original/adaptada correctamente explicada
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", sets: 3, restSeconds: 120);
            var accessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", sets: 4, restSeconds: 90, isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);
            var request = new SessionAdaptationRequest(routineDayId, AvailableMinutes: 18);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var explanation = SessionAdaptationExplanationGenerator.Generate(proposal);

            // Rationale must mention original duration, adapted duration, and available limit
            Assert.Contains("18 minutos disponibles", explanation.Rationale);
            Assert.Contains("tiempo máximo disponible", explanation.Rationale);
            Assert.Contains("se adaptó a aproximadamente", explanation.Rationale);
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

        private static Guid Id(int value) => new($"00000000-0000-0000-0000-{value:D12}");
        private static Guid Equip(int value) => new($"00000000-0000-0000-0001-{value:D12}");
    }
}
