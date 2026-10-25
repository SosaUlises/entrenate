using Entrenate.Application.Coach.RoutinePlanner.Conversation;
using Entrenate.Application.Coach.RoutinePlanner.Explanations;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.UnitTests.Coach.RoutinePlanner.Explanations;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.Conversation
{
    public class RoutinePlannerConversationContextBuilderTests
    {
        [Fact]
        public void Build_ProjectsOnlySafeProposalContext()
        {
            var proposal = RoutinePlannerExplanationGeneratorTests
                .CreateProposal();
            var explanation = RoutinePlannerExplanationGenerator.Generate(
                proposal);

            var context = RoutinePlannerConversationContextBuilder.Build(
                proposal,
                explanation);

            Assert.Equal(
                ObjetivoEntrenamiento.GanarMasaMuscular,
                context.Goal);
            Assert.Equal(NivelExperiencia.Principiante,
                context.ExperienceLevel);
            Assert.Equal(2, context.DaysPerWeek);
            Assert.True(context.Feasibility.CanBeConfirmed);
            Assert.True(context.Feasibility.HasLimitations);
            Assert.Equal(2, context.Sessions.Count);
            Assert.True(context.Sessions[0].IsPreferredDay);
            Assert.False(context.Sessions[1].IsPreferredDay);
            Assert.Equal(50, context.Sessions[0].EstimatedDurationMinutes);
            Assert.Equal(
                "Press de banca",
                context.Sessions[0].Exercises.Single().Name);
            Assert.Equal(3, context.Sessions[0].Exercises.Single().Sets);
            Assert.Equal("Pecho",
                context.WeeklyMuscleVolumes.Single().MuscleGroup);
            Assert.Equal(explanation.Warnings, context.Diagnostics);
            Assert.Same(explanation, context.Explanation);

            Assert.DoesNotContain(
                context.GetType().GetProperties(),
                property => property.Name.Contains(
                    "Id",
                    StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains(
                        "Hash",
                        StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(
                typeof(RoutinePlannerConversationExercise).GetProperties(),
                property => property.Name.Contains(
                    "Id",
                    StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals(
                        "Code",
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
