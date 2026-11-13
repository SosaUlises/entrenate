using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Routing;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Routing
{
    public sealed class LiveCoachCombinedSignalsIntentRouterTests
    {
        private readonly LiveCoachIntentRouter _router = new();

        [Theory]
        [InlineData("estoy cansado y me quedan 20 minutos", 20, LiveFatigueSeverity.Moderate)]
        [InlineData("estoy muy fatigado y solo tengo 15 minutos", 15, LiveFatigueSeverity.High)]
        public void Route_TimeAndFatigue_PreservesBothSignals(
            string text,
            int minutes,
            LiveFatigueSeverity severity)
        {
            var result = _router.Route(text);

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationCombinedSignals, result.Kind);
            Assert.Equal(minutes, result.RemainingMinutes);
            Assert.Equal(severity, result.FatigueSeverity);
        }

        [Fact]
        public void Route_TimeAndPerformance_PreservesBothSignals()
        {
            var result = _router.Route("hoy estoy más débil y me quedan 20 minutos");

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationCombinedSignals, result.Kind);
            Assert.Equal(20, result.RemainingMinutes);
            Assert.Equal(LivePerformanceSignalType.PerceivedWeakness, result.PerformanceSignalType);
        }

        [Fact]
        public void Route_TripleSignal_PreservesEverySignal()
        {
            var result = _router.Route(
                "estoy muy cansado, no llego a las reps y me quedan 10 minutos");

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationCombinedSignals, result.Kind);
            Assert.Equal(10, result.RemainingMinutes);
            Assert.Equal(LiveFatigueSeverity.High, result.FatigueSeverity);
            Assert.Equal(LivePerformanceSignalType.MissedRepetitions, result.PerformanceSignalType);
        }

        [Fact]
        public void Route_WhenUserRejectsCuts_DoesNotCreateAdaptationIntent()
        {
            var result = _router.Route(
                "estoy cansado pero no quiero recortar nada y me quedan 20 minutos");

            Assert.Equal(LiveCoachIntentKind.ConversationalQuestion, result.Kind);
            Assert.Contains("sin recortes", result.DirectAnswer);
        }

        [Theory]
        [InlineData(LiveCoachActionTypes.RegisterCurrentSet)]
        [InlineData(LiveCoachActionTypes.SkipCurrentExercise)]
        [InlineData(LiveCoachActionTypes.ReactivateExercise)]
        public void Route_WhenDifferentMutationIsPending_BlocksAdaptation(string pendingType)
        {
            var result = _router.Route("estoy cansado", pendingType);

            Assert.Equal(LiveCoachIntentKind.ConversationalQuestion, result.Kind);
            Assert.Contains("confirmá o cancelá", result.DirectAnswer);
        }

        [Fact]
        public void Route_WhenAdaptationIsPending_AllowsReplacementSignal()
        {
            var result = _router.Route(
                "me quedan 20 minutos",
                LiveCoachActionTypes.ApplyFatigueAdaptation);

            Assert.Equal(LiveCoachIntentKind.LiveAdaptationTimeConstraint, result.Kind);
            Assert.Equal(20, result.RemainingMinutes);
        }
    }
}
