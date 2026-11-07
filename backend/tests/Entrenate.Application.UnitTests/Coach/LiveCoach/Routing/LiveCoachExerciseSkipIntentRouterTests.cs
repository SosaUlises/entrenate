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
        [InlineData("no quiero hacer este ejercicio")]
        [InlineData("no quiero hacer este, pasá al siguiente")]
        public void Route_ExplicitSkipPhrase_ReturnsSkipIntent(string phrase)
        {
            var result = new LiveCoachIntentRouter().Route(phrase);

            Assert.Equal(LiveCoachIntentKind.SkipCurrentExercise, result.Kind);
        }

        [Theory]
        [InlineData("mostrame el siguiente ejercicio", LiveCoachIntentKind.NavigateNextExercise)]
        [InlineData("pasá al siguiente", LiveCoachIntentKind.NavigateNextExercise)]
        [InlineData("siguiente ejercicio", LiveCoachIntentKind.NavigateNextExercise)]
        [InlineData("volvé al ejercicio anterior", LiveCoachIntentKind.NavigatePreviousExercise)]
        [InlineData("mostrame el anterior", LiveCoachIntentKind.NavigatePreviousExercise)]
        [InlineData("abrí hip thrust", LiveCoachIntentKind.NavigateToExercise)]
        [InlineData("llevame a curl femoral", LiveCoachIntentKind.NavigateToExercise)]
        public void Route_VisualNavigationPhrase_ReturnsNavigationIntent(
            string phrase,
            LiveCoachIntentKind expected)
        {
            var result = new LiveCoachIntentRouter().Route(phrase);

            Assert.Equal(expected, result.Kind);
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
