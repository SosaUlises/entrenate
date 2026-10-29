using System.Text.Json;
using System.Text.RegularExpressions;
using Entrenate.Application.Coach.AdaptiveTraining.Conversation;
using Entrenate.Application.Coach.AdaptiveTraining.Explanations;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Coach.AdaptiveTraining.TimeAdaptation;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining.Conversation
{
    public sealed class SessionAdaptationConversationContextTests
    {
        [Fact]
        public void Build_NullProposal_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SessionAdaptationConversationContextBuilder.Build(null!));
        }

        [Fact]
        public void Build_ValidProposal_BuildsStructuredHumanReadableContext()
        {
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", sets: 3);
            var accessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", sets: 3, isRequiredIntent: false);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 30,
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request);
            var context = SessionAdaptationConversationContextBuilder.Build(proposal);

            Assert.NotNull(context);
            Assert.Equal("Factible con adaptaciones", context.Feasibility);
            Assert.True(context.CanBeConfirmed);
            Assert.Equal(30, context.AvailableMinutes);
            Assert.Equal("Más liviana", context.EffortPreference);
            Assert.Equal(2, context.OriginalExercises.Count);
            Assert.Equal(2, context.AdaptedExercises.Count);
            Assert.NotEmpty(context.Changes);
            Assert.NotNull(context.Explanation);

            // Exercise roles are natural
            Assert.Equal("Compuesto principal requerido", context.OriginalExercises[0].Role);
            Assert.Equal("Accesorio opcional", context.OriginalExercises[1].Role);
        }

        [Fact]
        public void Build_ConversationContext_DoesNotExposeInternalIdsHashesCodesOrEnums()
        {
            // Case 13: contexto no contiene IDs/hash/policy/codes internos
            var routineDayId = Guid.NewGuid();
            var compound = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", requiredEquip: [Equip(1)], sets: 3);
            var accessory = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", requiredEquip: [Equip(2)], sets: 3, isRequiredIntent: false);
            var candidate = CreateCandidate(3, "db_bench", "Dumbbell Bench", TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [compound, accessory]);
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 20,
                AvailableEquipmentIds: [Equip(2)],
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, [candidate]);
            var context = SessionAdaptationConversationContextBuilder.Build(proposal);

            var json = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });

            // 1. Must NOT contain ProposalId or ProposalHash
            Assert.DoesNotContain(proposal.AdaptationProposalId.ToString(), json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(proposal.ProposalHash, json, StringComparison.OrdinalIgnoreCase);

            // 2. Must NOT contain PolicyVersion
            Assert.DoesNotContain(proposal.PolicyVersion, json, StringComparison.OrdinalIgnoreCase);

            // 3. Must NOT contain internal diagnostic enum code names
            Assert.DoesNotContain("RequiredExerciseHasNoSubstitute", json);
            Assert.DoesNotContain("MinimumSessionExceedsAvailableTime", json);
            Assert.DoesNotContain("OptionalAccessoryRemovedForTime", json);
            Assert.DoesNotContain("ExerciseSetsReducedForTime", json);
            Assert.DoesNotContain("OptionalAccessorySetsReducedForLighter", json);
            Assert.DoesNotContain("NeutralLighterTransformation", json);

            // 4. Must NOT contain internal feasibility or priority enum names
            Assert.DoesNotContain("FeasibleWithChanges", json);
            Assert.DoesNotContain("RequiredCompound", json);
            Assert.DoesNotContain("OptionalAccessory", json);

            // 5. Must NOT contain raw GUIDs (36 characters 8-4-4-4-12)
            var guidPattern = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
            var matches = guidPattern.Matches(json);
            Assert.Empty(matches);
        }

        [Fact]
        public void GenerateAndBuild_AreDeterministic_IdenticalProposalsProduceIdenticalOutputs()
        {
            // Case 14: determinismo
            var routineDayId = Guid.NewGuid();
            var c1 = CreateExercise(1, 1, TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", requiredEquip: [Equip(1)], sets: 3);
            var c2 = CreateExercise(2, 2, TipoEjercicio.Aislamiento, PatronMovimiento.ExtensionCodo, "Tríceps", requiredEquip: [Equip(2)], sets: 3, isRequiredIntent: false);
            var cand = CreateCandidate(3, "db_bench", "Dumbbell Bench", TipoEjercicio.Compuesto, PatronMovimiento.EmpujeHorizontal, "Pecho", [Equip(2)]);

            var session = new PlannedAdaptiveSession(routineDayId, [c1, c2]);
            var request = new SessionAdaptationRequest(
                routineDayId,
                AvailableMinutes: 20,
                AvailableEquipmentIds: [Equip(2)],
                EffortPreference: SessionEffortPreference.Lighter);

            var proposal = SessionAdaptationProposalEngine.Adapt(session, request, [cand]);

            // Call 1
            var explanation1 = SessionAdaptationExplanationGenerator.Generate(proposal);
            var context1 = SessionAdaptationConversationContextBuilder.Build(proposal, explanation1);

            // Call 2
            var explanation2 = SessionAdaptationExplanationGenerator.Generate(proposal);
            var context2 = SessionAdaptationConversationContextBuilder.Build(proposal, explanation2);

            Assert.Equal(explanation1.Summary, explanation2.Summary);
            Assert.Equal(explanation1.Rationale, explanation2.Rationale);
            Assert.Equal(explanation1.Changes, explanation2.Changes);
            Assert.Equal(explanation1.Warnings, explanation2.Warnings);

            var json1 = JsonSerializer.Serialize(context1);
            var json2 = JsonSerializer.Serialize(context2);
            Assert.Equal(json1, json2);
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

