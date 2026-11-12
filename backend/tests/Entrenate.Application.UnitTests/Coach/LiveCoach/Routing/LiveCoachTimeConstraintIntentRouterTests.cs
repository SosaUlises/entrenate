using Entrenate.Application.Coach.LiveCoach.Routing;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Routing
{
    public sealed class LiveCoachTimeConstraintIntentRouterTests
    {
        private readonly LiveCoachIntentRouter _router = new();

        [Theory]
        [InlineData("me quedan 20 minutos", 20)]
        [InlineData("tengo 15 minutos más", 15)]
        [InlineData("solo puedo entrenar 10 minutos más", 10)]
        [InlineData("me tengo que ir en 30", 30)]
        [InlineData("quiero terminar en 25 minutos", 25)]
        [InlineData("dispongo de 40 minutos más", 40)]
        [InlineData("me quedan veinte minutos disponibles para entrenar", 20)]
        [InlineData("solo tengo quince minutos para entrenar", 15)]
        [InlineData("dispongo de treinta minutos", 30)]
        [InlineData("me tengo que ir en veinticinco minutos", 25)]
        [InlineData("quiero terminar en veinte minutos", 20)]
        [InlineData("tengo veinte minutos disponibles", 20)]
        [InlineData("puedo entrenar cuarenta y cinco minutos más", 45)]
        [InlineData("me quedan cinco minutos", 5)]
        [InlineData("me quedan diez minutos", 10)]
        [InlineData("me quedan cuarenta minutos", 40)]
        [InlineData("me quedan sesenta minutos", 60)]
        public void Route_WhenCurrentTimeConstraintIsExplicit_ReturnsAdaptation(
            string text,
            int expectedMinutes)
        {
            var result = _router.Route(text);

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationTimeConstraint, result.Kind);
            Assert.Equal(expectedMinutes, result.RemainingMinutes);
        }

        [Theory]
        [InlineData("¿cuánto me falta para terminar?", LiveCoachIntentKind.SessionProgress)]
        [InlineData("¿cuánto descanso me queda?", LiveCoachIntentKind.RestTimerQuery)]
        [InlineData("iniciá 20 minutos de descanso", LiveCoachIntentKind.UnsupportedRestDurationMutation)]
        public void Route_DoesNotStealExistingTimeRelatedIntents(
            string text,
            LiveCoachIntentKind expected)
        {
            Assert.Equal(expected, _router.Route(text).Kind);
        }

        [Fact]
        public void Route_WhenDurationIsHabitual_DoesNotTreatItAsLiveAdaptation()
        {
            var result = _router.Route("entreno 60 minutos por día");

            Assert.NotEqual(LiveCoachIntentKind.LiveAdaptationTimeConstraint, result.Kind);

            result = _router.Route("mi rutina dura sesenta minutos");

            Assert.NotEqual(LiveCoachIntentKind.LiveAdaptationTimeConstraint, result.Kind);
        }
    }
}
