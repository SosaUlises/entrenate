using Entrenate.Application.Coach.RoutinePlanner.Explanations;
using Entrenate.Application.Coach.RoutinePlanner.ExerciseSelection;
using Entrenate.Application.Coach.RoutinePlanner.Prescription;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.Explanations
{
    public class RoutinePlannerExplanationGeneratorTests
    {
        [Theory]
        [InlineData(RoutineProposalFeasibility.Feasible, 0)]
        [InlineData(RoutineProposalFeasibility.FeasibleWithWarnings, 1)]
        [InlineData(RoutineProposalFeasibility.NotFeasible, 2)]
        public void Generate_ExplainsProposalWithoutInternalTerminology(
            RoutineProposalFeasibility feasibility,
            int expectedWarningCount)
        {
            var proposal = CreateProposal(feasibility);

            var explanation = RoutinePlannerExplanationGenerator.Generate(
                proposal);

            Assert.NotEmpty(explanation.Summary);
            Assert.Contains("cuerpo completo", explanation.Structure);
            Assert.Contains("nivel principiante", explanation.Rationale);
            Assert.Equal(expectedWarningCount, explanation.Warnings.Count);

            var completeText = string.Join(
                " ",
                new[]
                {
                    explanation.Summary,
                    explanation.Structure,
                    explanation.Rationale
                }.Concat(explanation.Warnings));

            Assert.DoesNotContain(
                nameof(RoutineProposalFeasibility.FeasibleWithWarnings),
                completeText,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                nameof(RoutineProposalFeasibility.NotFeasible),
                completeText,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                nameof(ExerciseSelectionDiagnosticCode
                    .ExerciseReusedAcrossVariants),
                completeText,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                nameof(RoutinePrescriptionDiagnosticCode
                    .SelectionNotFeasible),
                completeText,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                proposal.ProposalId.ToString(),
                completeText,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                proposal.CanonicalHash,
                completeText,
                StringComparison.OrdinalIgnoreCase);
        }

        internal static RoutinePlannerProposal CreateProposal(
            RoutineProposalFeasibility feasibility =
                RoutineProposalFeasibility.FeasibleWithWarnings)
        {
            var selectionDiagnostics = feasibility ==
                RoutineProposalFeasibility.Feasible
                    ? []
                    : new[]
                    {
                        new ExerciseSelectionDiagnostic(
                            feasibility ==
                                RoutineProposalFeasibility.NotFeasible
                                    ? ExerciseSelectionDiagnosticCode
                                        .MissingRequiredExercise
                                    : ExerciseSelectionDiagnosticCode
                                        .ExerciseReusedAcrossVariants,
                            1,
                            1)
                    };
            var prescriptionDiagnostics = feasibility ==
                RoutineProposalFeasibility.NotFeasible
                    ? new[]
                    {
                        new RoutinePrescriptionDiagnostic(
                            RoutinePrescriptionDiagnosticCode
                                .SelectionNotFeasible)
                    }
                    : [];

            return new RoutinePlannerProposal(
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                "2.4",
                new string('a', 64),
                feasibility,
                ObjetivoEntrenamiento.GanarMasaMuscular,
                NivelExperiencia.Principiante,
                2,
                [
                    CreateDay(
                        1,
                        DiaSemana.Lunes,
                        ScheduledDayOrigin.Preferred,
                        RoutineSessionVariant.A),
                    CreateDay(
                        2,
                        DiaSemana.Jueves,
                        ScheduledDayOrigin.Derived,
                        RoutineSessionVariant.B)
                ],
                [new WeeklyMuscleVolume("Pecho", 6m)],
                selectionDiagnostics,
                prescriptionDiagnostics);
        }

        private static RoutinePlannerProposalDay CreateDay(
            int order,
            DiaSemana day,
            ScheduledDayOrigin origin,
            RoutineSessionVariant variant)
        {
            return new RoutinePlannerProposalDay(
                order,
                day,
                origin,
                RoutineSessionFocus.FullBody,
                variant,
                60,
                3000,
                [
                    new RoutinePlannerProposalExercise(
                        Guid.Parse(
                            $"20000000-0000-0000-0000-{order:D12}"),
                        $"press-{order}",
                        "Press de banca",
                        1,
                        PatronMovimiento.EmpujeHorizontal,
                        TipoEjercicio.Compuesto,
                        ExerciseSlotRole.PrimaryCompound,
                        3,
                        8,
                        12,
                        2,
                        3,
                        120)
                ]);
        }
    }
}
