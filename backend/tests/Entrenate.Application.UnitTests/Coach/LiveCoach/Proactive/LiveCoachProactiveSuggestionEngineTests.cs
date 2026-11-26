using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.Conversation;
using Entrenate.Application.Coach.LiveCoach.Proactive;
using Entrenate.Domain.Entidades;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Proactive
{
    public sealed class LiveCoachProactiveSuggestionEngineTests
    {
        private static readonly DateTime NowUtc = new(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Evaluate_TwoConsecutiveMissedSets_TriggersUnderperformance()
        {
            var session = CreateSession();
            Register(session.Ejercicios.First(), 1, 6, 2, NowUtc.AddMinutes(-2));
            Register(session.Ejercicios.First(), 2, 7, 2, NowUtc.AddMinutes(-1));

            var result = LiveCoachProactiveSuggestionEngine.Evaluate(session, null, null, NowUtc);

            Assert.Equal(
                LiveCoachProactiveTriggerType.RepeatedUnderperformance,
                result.First().Type);
            Assert.Equal("Revisar prescripción", result.First().ActionLabel);
        }

        [Fact]
        public void Evaluate_OneMissedSet_DoesNotTriggerUnderperformance()
        {
            var session = CreateSession();
            Register(session.Ejercicios.First(), 1, 6, 2, NowUtc.AddMinutes(-1));

            var result = LiveCoachProactiveSuggestionEngine.Evaluate(session, null, null, NowUtc);

            Assert.DoesNotContain(result, item =>
                item.Type == LiveCoachProactiveTriggerType.RepeatedUnderperformance);
        }

        [Fact]
        public void Evaluate_TwoConsecutiveLowRirSets_TriggersExcessiveEffort()
        {
            var session = CreateSession();
            Register(session.Ejercicios.First(), 1, 9, 0, NowUtc.AddMinutes(-2));
            Register(session.Ejercicios.First(), 2, 9, 0, NowUtc.AddMinutes(-1));

            var result = LiveCoachProactiveSuggestionEngine.Evaluate(session, null, null, NowUtc);

            Assert.Contains(result, item =>
                item.Type == LiveCoachProactiveTriggerType.ExcessiveEffort);
        }

        [Fact]
        public void Evaluate_LastTwoMissesWithNoPendingWork_DoesNotSuggestEvenWithOldRestSnapshot()
        {
            var session = CreateSession(exerciseCount: 1, setsPerExercise: 2);
            var exercise = session.Ejercicios.Single();
            Register(exercise, 1, 6, 0, NowUtc.AddMinutes(-2));
            Register(exercise, 2, 7, 0, NowUtc.AddMinutes(-1));

            Assert.Empty(LiveCoachProactiveSuggestionEngine.Evaluate(
                session, new(exercise.Id, NowUtc.AddMinutes(-5)), null, NowUtc));
        }

        [Fact]
        public void Evaluate_AtPrescriptionLimits_DoesNotOfferUnavailableAdjustments()
        {
            var session = CreateSession(repMin: 4, repMax: 4, rirMin: 5, rirMax: 5);
            var exercise = session.Ejercicios.First();
            Register(exercise, 1, 3, 0, NowUtc.AddMinutes(-2));
            Register(exercise, 2, 3, 0, NowUtc.AddMinutes(-1));

            Assert.Empty(LiveCoachProactiveSuggestionEngine.Evaluate(session, null, null, NowUtc));
        }

        [Fact]
        public void Evaluate_LongRest_UsesPrescribedRestPlusSixtySeconds()
        {
            var session = CreateSession(restSeconds: 90);
            var exercise = session.Ejercicios.First();
            var beforeThreshold = new LiveCoachProactiveSnapshot(
                exercise.Id,
                NowUtc.AddSeconds(-149));
            var atThreshold = beforeThreshold with
            {
                RestStartedAtUtc = NowUtc.AddSeconds(-150)
            };

            var before = LiveCoachProactiveSuggestionEngine.Evaluate(
                session, beforeThreshold, null, NowUtc);
            var after = LiveCoachProactiveSuggestionEngine.Evaluate(
                session, atThreshold, null, NowUtc);

            Assert.DoesNotContain(before, item => item.Type == LiveCoachProactiveTriggerType.LongRest);
            Assert.Contains(after, item => item.Type == LiveCoachProactiveTriggerType.LongRest);
        }

        [Fact]
        public void Evaluate_TimeDrift_RequiresRecentConstraint()
        {
            var session = CreateSession(exerciseCount: 4, setsPerExercise: 4);

            var withoutConstraint = LiveCoachProactiveSuggestionEngine.Evaluate(
                session, null, null, NowUtc);
            var withConstraint = LiveCoachProactiveSuggestionEngine.Evaluate(
                session,
                null,
                new LiveCoachTimeConstraintReference(10, NowUtc),
                NowUtc);

            Assert.DoesNotContain(withoutConstraint, item =>
                item.Type == LiveCoachProactiveTriggerType.SessionTimeDrift);
            Assert.Contains(withConstraint, item =>
                item.Type == LiveCoachProactiveTriggerType.SessionTimeDrift);

            var expired = LiveCoachProactiveSuggestionEngine.Evaluate(
                session,
                null,
                new LiveCoachTimeConstraintReference(30, NowUtc.AddMinutes(-21)),
                NowUtc);
            Assert.DoesNotContain(expired, item =>
                item.Type == LiveCoachProactiveTriggerType.SessionTimeDrift);
        }

        [Fact]
        public void Evaluate_UsesRequiredPriorityWhenSignalsCoincide()
        {
            var session = CreateSession(exerciseCount: 4, setsPerExercise: 4);
            var exercise = session.Ejercicios.First();
            Register(exercise, 1, 6, 0, NowUtc.AddMinutes(-2));
            Register(exercise, 2, 7, 0, NowUtc.AddMinutes(-1));

            var result = LiveCoachProactiveSuggestionEngine.Evaluate(
                session,
                new LiveCoachProactiveSnapshot(exercise.Id, NowUtc.AddMinutes(-5)),
                new LiveCoachTimeConstraintReference(10, NowUtc),
                NowUtc);

            Assert.Equal(
                LiveCoachProactiveTriggerType.RepeatedUnderperformance,
                result.First().Type);
        }

        [Fact]
        public void Evaluate_BlockedInteractionProducesNoSuggestion()
        {
            var session = CreateSession();
            Register(session.Ejercicios.First(), 1, 6, 2, NowUtc.AddMinutes(-2));
            Register(session.Ejercicios.First(), 2, 7, 2, NowUtc.AddMinutes(-1));

            var pendingBlocked = LiveCoachProactiveSuggestionEngine.Evaluate(
                session, null, null, NowUtc, interactionBlocked: true);
            var clarificationBlocked = LiveCoachProactiveSuggestionEngine.Evaluate(
                session, null, null, NowUtc, interactionBlocked: true);

            Assert.Empty(pendingBlocked);
            Assert.Empty(clarificationBlocked);
        }

        [Fact]
        public void CanEmit_DeduplicatesFingerprintAndAppliesCooldownPerExercise()
        {
            var firstExercise = Guid.NewGuid();
            var candidate = Candidate(firstExercise, "state-2");
            var recent = new[]
            {
                new LiveCoachProactiveTriggerReference(
                    candidate.Type,
                    firstExercise,
                    "state-1",
                    NowUtc.AddMinutes(-2),
                    "evidence")
            };

            Assert.False(LiveCoachProactiveSuggestionEngine.CanEmit(
                candidate with { SourceFingerprint = "state-1" }, recent, NowUtc));
            Assert.False(LiveCoachProactiveSuggestionEngine.CanEmit(candidate, recent, NowUtc));
            Assert.True(LiveCoachProactiveSuggestionEngine.CanEmit(
                candidate, recent, NowUtc.AddMinutes(5)));
            Assert.True(LiveCoachProactiveSuggestionEngine.CanEmit(
                candidate with { ExerciseSessionId = Guid.NewGuid() }, recent, NowUtc));
        }

        private static LiveCoachProactiveCandidate Candidate(Guid exerciseId, string fingerprint)
            => new(
                LiveCoachProactiveTriggerType.RepeatedUnderperformance,
                exerciseId,
                fingerprint,
                "text",
                "evidence");

        private static SesionEntrenamiento CreateSession(
            int restSeconds = 90,
            int exerciseCount = 2,
            int setsPerExercise = 3,
            int repMin = 8, int repMax = 10,
            int rirMin = 2, int rirMax = 3)
        {
            var exercises = Enumerable.Range(1, exerciseCount)
                .Select(index =>
                {
                    var exercise = new Ejercicio
                    {
                        Id = Guid.NewGuid(),
                        Nombre = $"Ejercicio {index}",
                        GrupoMuscularPrincipal = "General",
                        Activo = true
                    };
                    var sessionExercise = new EjercicioSesion(
                        exercise.Id,
                        index,
                        setsPerExercise,
                        repMin,
                        repMax,
                        rirMin,
                        rirMax,
                        restSeconds);
                    typeof(EjercicioSesion)
                        .GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)!
                        .SetValue(sessionExercise, exercise);
                    return sessionExercise;
                })
                .ToArray();
            return new SesionEntrenamiento(
                "user-1",
                Guid.NewGuid(),
                NowUtc.AddMinutes(-30),
                exercises);
        }

        private static void Register(
            EjercicioSesion exercise,
            int number,
            int repetitions,
            int rir,
            DateTime registeredAtUtc)
            => exercise.RegistrarOActualizarSerie(
                number,
                100,
                repetitions,
                rir,
                registeredAtUtc);
    }
}
