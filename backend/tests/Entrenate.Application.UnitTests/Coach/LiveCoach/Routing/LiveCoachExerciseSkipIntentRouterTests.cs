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
        [InlineData("mostrame el siguiente", LiveCoachIntentKind.NavigateNextExercise)]
        [InlineData("pasá al siguiente", LiveCoachIntentKind.NavigateNextExercise)]
        [InlineData("siguiente ejercicio", LiveCoachIntentKind.NavigateNextExercise)]
        [InlineData("volvé al ejercicio anterior", LiveCoachIntentKind.NavigatePreviousExercise)]
        [InlineData("volvé al anterior", LiveCoachIntentKind.NavigatePreviousExercise)]
        [InlineData("mostrame el anterior", LiveCoachIntentKind.NavigatePreviousExercise)]
        [InlineData("abrí hip thrust", LiveCoachIntentKind.NavigateToExercise)]
        [InlineData("llevame a curl femoral", LiveCoachIntentKind.NavigateToExercise)]
        [InlineData("volvé al ejercicio actual", LiveCoachIntentKind.ReturnToCurrentExercise)]
        [InlineData("volvé al actual", LiveCoachIntentKind.ReturnToCurrentExercise)]
        [InlineData("mostrame el que me toca", LiveCoachIntentKind.ReturnToCurrentExercise)]
        [InlineData("mostrame el primer ejercicio", LiveCoachIntentKind.NavigateFirstExercise)]
        [InlineData("mostrame el último ejercicio", LiveCoachIntentKind.NavigateLastExercise)]
        [InlineData("mostrame un ejercicio de espalda", LiveCoachIntentKind.NavigateToMuscleGroup)]
        [InlineData("mostrame uno de glúteos", LiveCoachIntentKind.NavigateToMuscleGroup)]
        [InlineData("mostrame el siguiente de espalda", LiveCoachIntentKind.NavigateNextByMuscleGroup)]
        public void Route_VisualNavigationPhrase_ReturnsNavigationIntent(
            string phrase,
            LiveCoachIntentKind expected)
        {
            var result = new LiveCoachIntentRouter().Route(phrase);

            Assert.Equal(expected, result.Kind);
        }

        [Fact]
        public void Route_ShowExerciseByName_RemainsNameNavigation()
        {
            var result = new LiveCoachIntentRouter().Route("mostrame hip thrust");

            Assert.Equal(LiveCoachIntentKind.NavigateToExercise, result.Kind);
        }

        [Theory]
        [InlineData("mostrame último", LiveCoachIntentKind.NavigateLastExercise)]
        [InlineData("volvé al primero", LiveCoachIntentKind.NavigateFirstExercise)]
        public void Route_ShortBoundaryNavigation_ReturnsBoundaryIntent(
            string phrase,
            LiveCoachIntentKind expected)
        {
            var result = new LiveCoachIntentRouter().Route(phrase);

            Assert.Equal(expected, result.Kind);
        }

        [Fact]
        public void Route_SkipNextExercise_IsNotVisualNavigation()
        {
            var result = new LiveCoachIntentRouter().Route("saltá el siguiente ejercicio");

            Assert.Equal(LiveCoachIntentKind.OtherMutation, result.Kind);
        }

        [Fact]
        public void Route_MuscleGroupListQuestion_IsNotVisualNavigation()
        {
            var result = new LiveCoachIntentRouter().Route("¿qué ejercicios de espalda tengo hoy?");

            Assert.NotEqual(LiveCoachIntentKind.NavigateToMuscleGroup, result.Kind);
            Assert.NotEqual(LiveCoachIntentKind.NavigateNextByMuscleGroup, result.Kind);
        }

        [Fact]
        public void Route_GenericNextExercise_DoesNotBecomeMuscleNavigation()
        {
            var result = new LiveCoachIntentRouter().Route("mostrame el siguiente ejercicio");

            Assert.Equal(LiveCoachIntentKind.NavigateNextExercise, result.Kind);
        }

        [Fact]
        public void Route_CurrentExerciseQuestion_ReturnsDeterministicQueryIntent()
        {
            var result = new LiveCoachIntentRouter().Route("¿qué ejercicio me toca?");

            Assert.Equal(LiveCoachIntentKind.CurrentExercise, result.Kind);
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
