using System.Text.Json;
using Entrenate.Application.Coach.Progress.Conversation;
using Entrenate.Application.Coach.Progress.Explanations;
using Entrenate.Application.Coach.Progress.Recommendations;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.Progress.Conversation
{
    public class ProgressConversationPromptBuilderTests
    {
        [Fact]
        public void Build_CreatesControlledInstructionsAndStructuredInput()
        {
            var exerciseId = Guid.Parse(
                "10000000-0000-0000-0000-000000000001");
            var context = new ProgressConversationContext(
                exerciseId,
                1,
                1,
                1,
                false,
                false,
                new ProgressConversationPrescription(3, 6, 10, 1, 2),
                null,
                ProgressDecision.Maintain,
                EvidenceLevel.Low,
                [ReasonCode.TopRangeReached],
                new ProgressExplanation(
                    "Alcanzaste el extremo superior del rango.",
                    "Mantené la carga.",
                    "Todavía falta otra exposición comparable."));
            var request = new ProgressConversationRequest(
                "  ¿Conviene subir peso?  ",
                context);

            var prompt = ProgressConversationPromptBuilder.Build(request);

            Assert.Contains(
                "ProgressDecision es la fuente de verdad",
                prompt.Instructions);
            Assert.Contains(
                "Nunca expongas al usuario nombres internos de enums, reason codes ni terminología de implementación",
                prompt.Instructions);
            Assert.Contains(
                "No inventes pesos, sesiones, repeticiones, RIR, métricas",
                prompt.Instructions);
            Assert.Contains("nutrición", prompt.Instructions);
            Assert.Contains("lesiones", prompt.Instructions);
            Assert.Contains("medicina", prompt.Instructions);
            Assert.Contains("No uses emojis", prompt.Instructions);
            Assert.Contains(
                "Respondé directamente a la pregunta",
                prompt.Instructions);
            Assert.Contains(
                "entre 2 y 4 párrafos breves",
                prompt.Instructions);
            Assert.Contains(
                "No omitas información necesaria",
                prompt.Instructions);
            Assert.Contains(
                "listas sólo cuando realmente mejoren la claridad",
                prompt.Instructions);
            Assert.Contains(
                "fácil de leer en mobile",
                prompt.Instructions);

            using var document = JsonDocument.Parse(prompt.Input);
            var root = document.RootElement;

            Assert.Equal(
                "¿Conviene subir peso?",
                root.GetProperty("question").GetString());
            Assert.Equal(
                exerciseId,
                root.GetProperty("context")
                    .GetProperty("exerciseId")
                    .GetGuid());
            Assert.Equal(
                "Maintain",
                root.GetProperty("context")
                    .GetProperty("decision")
                    .GetString());
            Assert.Equal(
                "TopRangeReached",
                root.GetProperty("context")
                    .GetProperty("reasonCodes")[0]
                    .GetString());
        }

        [Fact]
        public void Build_RejectsEmptyQuestion()
        {
            var context = new ProgressConversationContext(
                Guid.NewGuid(),
                0,
                0,
                0,
                false,
                false,
                null,
                null,
                ProgressDecision.InsufficientData,
                EvidenceLevel.Low,
                [ReasonCode.InsufficientHistory],
                new ProgressExplanation(
                    "Sin datos.",
                    "Registrá más sesiones.",
                    "No hay historial válido."));

            Assert.Throws<ArgumentException>(() =>
                ProgressConversationPromptBuilder.Build(
                    new ProgressConversationRequest("   ", context)));
        }
    }
}
