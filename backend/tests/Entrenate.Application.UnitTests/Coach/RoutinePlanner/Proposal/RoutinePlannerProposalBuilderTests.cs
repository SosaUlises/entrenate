using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.Proposal
{
    public class RoutinePlannerProposalBuilderTests
    {
        private static readonly Guid EquipmentA = CreateGuid(900);
        private static readonly Guid EquipmentB = CreateGuid(901);

        [Fact]
        public void Build_ReturnsCompleteFeasibleProposal()
        {
            var result = RoutinePlannerProposalBuilder.Build(CreateInput());

            Assert.Equal(RoutineProposalFeasibility.Feasible,
                result.Feasibility);
            Assert.Equal(6, result.Days.Count);
            Assert.All(result.Days, day =>
            {
                Assert.NotEmpty(day.Exercises);
                Assert.Equal(
                    Enumerable.Range(1, day.Exercises.Count),
                    day.Exercises.Select(exercise => exercise.Order));
                Assert.All(day.Exercises, exercise =>
                {
                    Assert.InRange(exercise.Sets, 2, 3);
                    Assert.True(exercise.MinReps <= exercise.MaxReps);
                    Assert.True(exercise.MinRir <= exercise.MaxRir);
                    Assert.InRange(exercise.RestSeconds, 15, 900);
                });
                Assert.True(day.EstimatedDurationSeconds > 0);
            });
            Assert.NotEmpty(result.WeeklyMuscleVolumes);
            Assert.Empty(result.SelectionDiagnostics);
            Assert.Empty(result.PrescriptionDiagnostics);
            Assert.Equal(64, result.CanonicalHash.Length);
            Assert.NotEqual(Guid.Empty, result.ProposalId);
        }

        [Fact]
        public void Build_PropagatesWarningsFromExistingEngines()
        {
            var input = CreateInput() with
            {
                Catalog = CreateCompleteCatalog()
                    .Where(candidate => candidate.Code!.EndsWith(
                        "-a",
                        StringComparison.Ordinal))
                    .ToArray()
            };

            var result = RoutinePlannerProposalBuilder.Build(input);

            Assert.Equal(
                RoutineProposalFeasibility.FeasibleWithWarnings,
                result.Feasibility);
            Assert.Contains(
                result.SelectionDiagnostics,
                diagnostic => diagnostic.Code ==
                    ExerciseSelectionDiagnosticCode
                        .ExerciseReusedAcrossVariants);
        }

        [Fact]
        public void Build_PropagatesNotFeasibleAndPreservesDiagnostics()
        {
            var input = CreateInput() with
            {
                Catalog = CreateCompleteCatalog()
                    .Where(candidate => candidate.MovementPattern !=
                        PatronMovimiento.TraccionVertical)
                    .ToArray()
            };

            var result = RoutinePlannerProposalBuilder.Build(input);

            Assert.Equal(
                RoutineProposalFeasibility.NotFeasible,
                result.Feasibility);
            Assert.Equal(6, result.Days.Count);
            Assert.Contains(
                result.SelectionDiagnostics,
                diagnostic => diagnostic.Code ==
                    ExerciseSelectionDiagnosticCode
                        .MissingRequiredExercise);
            Assert.Contains(
                result.PrescriptionDiagnostics,
                diagnostic => diagnostic.Code ==
                    RoutinePrescriptionDiagnosticCode
                        .SelectionNotFeasible);
        }

        [Fact]
        public void Build_SameInputProducesSameProposalIdAndHash()
        {
            var input = CreateInput();

            var first = RoutinePlannerProposalBuilder.Build(input);
            var second = RoutinePlannerProposalBuilder.Build(input);

            Assert.Equal(first.ProposalId, second.ProposalId);
            Assert.Equal(first.CanonicalHash, second.CanonicalHash);
        }

        [Fact]
        public void Build_RelevantInputChangeProducesDifferentIdentity()
        {
            var input = CreateInput();
            var changedInput = input with
            {
                TargetSessionDurationMinutes = 90
            };

            var first = RoutinePlannerProposalBuilder.Build(input);
            var changed = RoutinePlannerProposalBuilder.Build(changedInput);

            Assert.NotEqual(first.CanonicalHash, changed.CanonicalHash);
            Assert.NotEqual(first.ProposalId, changed.ProposalId);
        }

        [Fact]
        public void Build_IncidentalCollectionOrderDoesNotChangeProposal()
        {
            var input = CreateInput();
            var reorderedInput = input with
            {
                PreferredDays = input.PreferredDays.Reverse().ToArray(),
                AvailableEquipmentIds = input.AvailableEquipmentIds
                    .Reverse()
                    .ToArray(),
                Catalog = input.Catalog.Reverse().ToArray()
            };

            var first = RoutinePlannerProposalBuilder.Build(input);
            var reordered = RoutinePlannerProposalBuilder.Build(
                reorderedInput);

            Assert.Equal(first.CanonicalHash, reordered.CanonicalHash);
            Assert.Equal(first.ProposalId, reordered.ProposalId);
            Assert.Equal(
                first.Days.SelectMany(day => day.Exercises)
                    .Select(exercise => exercise.Code),
                reordered.Days.SelectMany(day => day.Exercises)
                    .Select(exercise => exercise.Code));
        }

        private static RoutinePlannerInput CreateInput()
        {
            return new RoutinePlannerInput(
                RoutinePlannerProposalBuilder.PolicyVersion,
                ObjetivoEntrenamiento.GanarFuerza,
                NivelExperiencia.Principiante,
                6,
                [
                    DiaSemana.Lunes,
                    DiaSemana.Martes,
                    DiaSemana.Miercoles,
                    DiaSemana.Jueves,
                    DiaSemana.Viernes,
                    DiaSemana.Sabado
                ],
                120,
                [EquipmentA, EquipmentB],
                CreateCompleteCatalog());
        }

        private static PlannerExerciseCandidate[] CreateCompleteCatalog()
        {
            var candidates = new List<PlannerExerciseCandidate>();
            var id = 1;

            AddPair("bench", "Pecho",
                PatronMovimiento.EmpujeHorizontal,
                TipoEjercicio.Compuesto,
                ["Tríceps"]);
            AddPair("press", "Hombros",
                PatronMovimiento.EmpujeVertical,
                TipoEjercicio.Compuesto,
                ["Tríceps"]);
            AddPair("row", "Espalda",
                PatronMovimiento.TraccionHorizontal,
                TipoEjercicio.Compuesto,
                ["Bíceps"]);
            AddPair("pull", "Espalda",
                PatronMovimiento.TraccionVertical,
                TipoEjercicio.Compuesto,
                ["Bíceps"]);
            AddPair("squat", "Cuádriceps",
                PatronMovimiento.DominanteRodilla,
                TipoEjercicio.Compuesto,
                ["Glúteos"]);
            AddPair("hinge", "Isquiotibiales",
                PatronMovimiento.BisagraCadera,
                TipoEjercicio.Compuesto,
                ["Glúteos"]);
            AddPair("lateral-raise", "Hombros",
                PatronMovimiento.AbduccionHombro,
                TipoEjercicio.Aislamiento);
            AddPair("triceps", "Tríceps",
                PatronMovimiento.ExtensionCodo,
                TipoEjercicio.Aislamiento);
            AddPair("biceps", "Bíceps",
                PatronMovimiento.FlexionCodo,
                TipoEjercicio.Aislamiento);
            AddPair("face-pull", "Hombros",
                PatronMovimiento.RotacionExternaHombro,
                TipoEjercicio.Aislamiento);
            AddPair("leg-extension", "Cuádriceps",
                PatronMovimiento.ExtensionRodilla,
                TipoEjercicio.Aislamiento);
            AddPair("leg-curl", "Isquiotibiales",
                PatronMovimiento.FlexionRodilla,
                TipoEjercicio.Aislamiento);
            AddPair("calf", "Gemelos",
                PatronMovimiento.FlexionPlantar,
                TipoEjercicio.Aislamiento);
            AddPair("core", "Core",
                PatronMovimiento.CoreAntiExtension,
                TipoEjercicio.Aislamiento);

            return candidates.ToArray();

            void AddPair(
                string code,
                string primaryMuscleGroup,
                PatronMovimiento pattern,
                TipoEjercicio type,
                IReadOnlyCollection<string>? secondaryMuscleGroups = null)
            {
                candidates.Add(Candidate(
                    id++,
                    $"{code}-a",
                    primaryMuscleGroup,
                    pattern,
                    type,
                    secondaryMuscleGroups));
                candidates.Add(Candidate(
                    id++,
                    $"{code}-b",
                    primaryMuscleGroup,
                    pattern,
                    type,
                    secondaryMuscleGroups));
            }
        }

        private static PlannerExerciseCandidate Candidate(
            int id,
            string code,
            string primaryMuscleGroup,
            PatronMovimiento pattern,
            TipoEjercicio type,
            IReadOnlyCollection<string>? secondaryMuscleGroups)
        {
            return new PlannerExerciseCandidate(
                CreateGuid(id),
                code,
                code,
                primaryMuscleGroup,
                secondaryMuscleGroups ?? [],
                pattern,
                type,
                true,
                []);
        }

        private static Guid CreateGuid(int value)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
        }
    }
}
