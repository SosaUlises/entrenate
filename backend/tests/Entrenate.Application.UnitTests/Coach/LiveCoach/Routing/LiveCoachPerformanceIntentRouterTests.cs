using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Routing
{
    public sealed class LiveCoachPerformanceIntentRouterTests
    {
        private readonly LiveCoachIntentRouter _router = new();

        [Theory]
        [InlineData("hoy estoy más débil", LivePerformanceSignalType.PerceivedWeakness)]
        [InlineData("hoy estoy flojo", LivePerformanceSignalType.PerceivedWeakness)]
        [InlineData("no tengo la misma fuerza", LivePerformanceSignalType.PerceivedWeakness)]
        [InlineData("no llego a las repeticiones", LivePerformanceSignalType.MissedRepetitions)]
        [InlineData("no puedo sacar las reps", LivePerformanceSignalType.MissedRepetitions)]
        [InlineData("este peso está demasiado pesado", LivePerformanceSignalType.ExcessiveEffort)]
        [InlineData("me está costando mucho", LivePerformanceSignalType.ExcessiveEffort)]
        public void Route_WhenCurrentPerformanceSignalIsExplicit_ReturnsClosedType(
            string text,
            LivePerformanceSignalType expected)
        {
            var result = _router.Route(text);

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationPerformanceSignal, result.Kind);
            Assert.Equal(expected, result.PerformanceSignalType);
        }

        [Theory]
        [InlineData("ayer estuve débil")]
        [InlineData("cuando entreno piernas quedo débil")]
        [InlineData("¿por qué estoy más débil?")]
        [InlineData("¿cómo puedo ser más fuerte?")]
        [InlineData("quiero ganar fuerza")]
        [InlineData("me duele el hombro")]
        [InlineData("sentí un pinchazo")]
        public void Route_WhenSignalIsHistoricalGenericOrPain_DoesNotCreatePerformanceAdaptation(
            string text)
        {
            Assert.NotEqual(
                LiveCoachIntentKind.LiveAdaptationPerformanceSignal,
                _router.Route(text).Kind);
        }

        [Fact]
        public void Route_WhenFatigueIsExplicit_KeepsFatigueIntent()
        {
            Assert.Equal(
                LiveCoachIntentKind.LiveAdaptationFatigueSignal,
                _router.Route("estoy muy fatigado").Kind);
        }

        [Fact]
        public void Route_WhenFatigueAndPerformanceAreCombined_ReturnsBothSignals()
        {
            var result = _router.Route("estoy cansado y hoy estoy más débil");

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationCombinedSignals, result.Kind);
            Assert.Equal(LiveFatigueSeverity.Moderate, result.FatigueSeverity);
            Assert.Equal(LivePerformanceSignalType.PerceivedWeakness, result.PerformanceSignalType);
        }
    }
}
