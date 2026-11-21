using System.Reflection;
using Entrenate.Application.Coach.LiveCoach.PostSession;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.PostSession
{
    public sealed class PostSessionCoachSummaryEngineTests
    {
        private static readonly DateTime NowUtc = new(2026, 11, 21, 18, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Create_AllCompletedWithinTargets_ReportsNoIncidentsAndPositiveWork()
        {
            var session = CreateSession(("Press", 2), ("Remo", 2));
            foreach (var exercise in session.Ejercicios)
            {
                Register(exercise, 1, 80, 9, 2);
                Register(exercise, 2, 80, 8, 1);
            }
            Complete(session);

            var summary = PostSessionCoachSummaryEngine.Create(session);

            Assert.Equal(2, summary.CompletedExercises);
            Assert.Equal(4, summary.CompletedSets);
            Assert.Equal(0, summary.UnderperformanceEvents);
            Assert.Equal(0, summary.ExcessiveEffortEvents);
            Assert.Contains(summary.NotableObservations, item => item.Contains("No hubo desvíos"));
            Assert.Contains("Press", PostSessionCoachAnswerResolver.Resolve(summary, "¿Qué salió bien?"));
        }

        [Fact]
        public void Create_OmittedExercise_IsReportedSeparatelyNotAsFailure()
        {
            var session = CreateSession(("Press", 2), ("Remo", 2));
            Register(session.Ejercicios.First(), 1, 80, 9, 2);
            Register(session.Ejercicios.First(), 2, 80, 9, 2);
            session.OmitirEjercicio(session.Ejercicios.Last().Id);
            Complete(session);

            var summary = PostSessionCoachSummaryEngine.Create(session);

            Assert.Equal(1, summary.CompletedExercises);
            Assert.Equal(1, summary.OmittedExercises);
            Assert.Equal(0, summary.UnderperformanceEvents);
        }

        [Fact]
        public void Create_UsesHistoricalTargetsForMissesEffortWorstAndNextTime()
        {
            var session = CreateSession(("Press", 3), ("Remo", 2));
            var press = session.Ejercicios.First();
            Register(press, 1, 100, 6, 0);
            Register(press, 2, 100, 7, 0);
            Register(press, 3, 90, 8, 1);
            var remo = session.Ejercicios.Last();
            Register(remo, 1, 70, 10, 2);
            Register(remo, 2, 70, 9, 2);
            Complete(session);

            var summary = PostSessionCoachSummaryEngine.Create(session);

            Assert.Equal(2, summary.UnderperformanceEvents);
            Assert.Equal(2, summary.ExcessiveEffortEvents);
            Assert.Contains("Press", PostSessionCoachAnswerResolver.Resolve(summary, "¿Dónde rendí peor?"));
            Assert.Contains("próxima", PostSessionCoachAnswerResolver.Resolve(summary, "¿Qué debería mirar la próxima vez?"));
        }

        [Fact]
        public void Create_ReturnsBestSetsByVolumeWeightAndRepetitions()
        {
            var session = CreateSession(("Press", 2));
            Register(session.Ejercicios.Single(), 1, 100, 8, 1);
            Register(session.Ejercicios.Single(), 2, 80, 12, 2);
            Complete(session);

            var summary = PostSessionCoachSummaryEngine.Create(session);

            Assert.Equal(3, summary.BestSets.Count);
            Assert.Equal(960, summary.BestSets.Single(item => item.Criterion == "Volume").Volume);
            Assert.Contains("80 kg por 12", PostSessionCoachAnswerResolver.Resolve(summary, "¿Cuál fue mi mejor serie?"));
        }

        [Fact]
        public void Create_OnlyCountsAppliedAdaptationsAndDescribesPrescriptionAudit()
        {
            var session = CreateSession(("Hip Thrust", 4));
            Register(session.Ejercicios.Single(), 1, 100, 6, 0);
            Register(session.Ejercicios.Single(), 2, 100, 6, 0);
            session.RegistrarAdaptacionLiveCoach(TipoAdaptacionLiveCoach.Prescripcion, NowUtc,
                session.Ejercicios.Single().Id, 3, 8, 10, 6, 8, 1, 2, 1, 2);
            Complete(session);

            var summary = PostSessionCoachSummaryEngine.Create(session);

            var adaptation = Assert.Single(summary.AdaptationsApplied);
            Assert.Equal("PrescriptionAdaptation", adaptation.Type);
            Assert.Contains("desde la serie 3", adaptation.Detail);
            Assert.Contains("Se aplicó 1 ajuste", PostSessionCoachAnswerResolver.Resolve(summary, "¿Qué adaptaciones hicimos?"));
        }

        [Fact]
        public void Create_NoAuditMeansCancelledProposalDoesNotCount()
        {
            var session = CreateSession(("Press", 1));
            Register(session.Ejercicios.Single(), 1, 80, 8, 1);
            Complete(session);

            var summary = PostSessionCoachSummaryEngine.Create(session);

            Assert.Empty(summary.AdaptationsApplied);
            Assert.Equal("No se aplicaron adaptaciones durante esta sesión.",
                PostSessionCoachAnswerResolver.Resolve(summary, "¿Qué adaptaciones hicimos?"));
        }

        private static SesionEntrenamiento CreateSession(params (string Name, int Sets)[] values)
        {
            var exercises = values.Select((value, index) =>
            {
                var definition = new Ejercicio
                {
                    Id = Guid.NewGuid(), Nombre = value.Name,
                    GrupoMuscularPrincipal = "General", Activo = true
                };
                var exercise = new EjercicioSesion(definition.Id, index + 1, value.Sets, 8, 10, 1, 2, 90);
                typeof(EjercicioSesion).GetProperty("Ejercicio", BindingFlags.Public | BindingFlags.Instance)!
                    .SetValue(exercise, definition);
                return exercise;
            });
            return new SesionEntrenamiento("user", Guid.NewGuid(), NowUtc.AddHours(-1), exercises);
        }

        private static void Register(EjercicioSesion exercise, int number, decimal weight, int reps, int rir)
            => exercise.RegistrarOActualizarSerie(number, weight, reps, rir, NowUtc.AddMinutes(number));

        private static void Complete(SesionEntrenamiento session) => session.Completar(NowUtc);
    }
}
