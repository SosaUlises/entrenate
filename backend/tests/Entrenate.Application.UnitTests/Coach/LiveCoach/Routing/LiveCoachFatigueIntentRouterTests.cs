using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Routing
{
    public sealed class LiveCoachFatigueIntentRouterTests
    {
        private readonly LiveCoachIntentRouter _router = new();

        [Theory]
        [InlineData("estoy cansado", LiveFatigueSeverity.Moderate)]
        [InlineData("me siento fatigada", LiveFatigueSeverity.Moderate)]
        [InlineData("hoy estoy bastante cansado", LiveFatigueSeverity.Moderate)]
        [InlineData("estoy muy cansado", LiveFatigueSeverity.High)]
        [InlineData("estoy muy fatigada", LiveFatigueSeverity.High)]
        [InlineData("estoy fundido", LiveFatigueSeverity.High)]
        [InlineData("no me da más el cuerpo", LiveFatigueSeverity.High)]
        [InlineData("estoy hecho mierda", LiveFatigueSeverity.High)]
        public void Route_WhenCurrentFatigueIsExplicit_ReturnsDeterministicSeverity(
            string text,
            LiveFatigueSeverity expected)
        {
            var result = _router.Route(text);

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationFatigueSignal, result.Kind);
            Assert.Equal(expected, result.FatigueSeverity);
        }

        [Theory]
        [InlineData("ayer estaba muy cansado")]
        [InlineData("cuando estoy cansado entreno menos")]
        [InlineData("la rutina me deja cansado")]
        [InlineData("¿es normal estar cansado?")]
        [InlineData("estoy cansado y me duele el hombro")]
        [InlineData("estoy cansado por una lesión")]
        public void Route_WhenSignalIsHistoricalGenericOrPain_DoesNotCreateFatigueAdaptation(
            string text)
        {
            Assert.NotEqual(
                LiveCoachIntentKind.LiveAdaptationFatigueSignal,
                _router.Route(text).Kind);
        }

        [Fact]
        public void Route_WhenTimeAndFatigueAreCombined_ReturnsBothSignals()
        {
            var result = _router.Route("estoy muy cansado y me quedan 20 minutos");

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationCombinedSignals, result.Kind);
            Assert.Equal(20, result.RemainingMinutes);
            Assert.Equal(LiveFatigueSeverity.High, result.FatigueSeverity);
        }
    }
}
