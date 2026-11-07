using Entrenate.Application.Coach.LiveCoach.Routing;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Routing
{
    public sealed class LiveCoachExerciseSkipIntentRouterTests
    {
        [Theory]
        [InlineData("saltá este ejercicio")]
        [InlineData("saltemos este ejercicio")]
        [InlineData("omití este ejercicio")]
        [InlineData("pasá al siguiente ejercicio")]
        [InlineData("no quiero hacer este ejercicio")]
        public void Route_ExplicitSkipPhrase_ReturnsSkipIntent(string phrase)
        {
            var result = new LiveCoachIntentRouter().Route(phrase);

            Assert.Equal(LiveCoachIntentKind.SkipCurrentExercise, result.Kind);
        }

        [Fact]
        public void Route_NextExerciseQuestion_RemainsReadOnly()
        {
            var result = new LiveCoachIntentRouter().Route("¿qué ejercicio sigue?");

            Assert.Equal(LiveCoachIntentKind.NextExercise, result.Kind);
        }

        [Theory]
        [InlineData("reactivá hip thrust")]
        [InlineData("volvé a activar hip thrust")]
        [InlineData("quiero volver a hacer hip thrust")]
        [InlineData("recuperá hip thrust")]
        [InlineData("sacá hip thrust de omitidos")]
        [InlineData("volvamos a hip thrust")]
        public void Route_ExplicitReactivationPhrase_ReturnsReactivateIntent(string phrase)
        {
            var result = new LiveCoachIntentRouter().Route(phrase);

            Assert.Equal(LiveCoachIntentKind.ReactivateExercise, result.Kind);
        }
    }
}
