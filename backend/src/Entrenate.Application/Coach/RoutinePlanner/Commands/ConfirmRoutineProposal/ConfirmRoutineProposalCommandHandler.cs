using Entrenate.Application.Coach.RoutinePlanner.DTOs;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using MediatR;

namespace Entrenate.Application.Coach.RoutinePlanner.Commands.ConfirmRoutineProposal
{
    public sealed class ConfirmRoutineProposalCommandHandler
        : IRequestHandler<
            ConfirmRoutineProposalCommand,
            RoutinePlannerConfirmationDto>
    {
        private const string RoutineName = "Rutina Entrenate Coach";

        private readonly ICurrentUserService _currentUserService;
        private readonly RoutinePlannerProposalGenerator _proposalGenerator;
        private readonly IRoutineRepository _routineRepository;

        public ConfirmRoutineProposalCommandHandler(
            ICurrentUserService currentUserService,
            RoutinePlannerProposalGenerator proposalGenerator,
            IRoutineRepository routineRepository)
        {
            _currentUserService = currentUserService;
            _proposalGenerator = proposalGenerator;
            _routineRepository = routineRepository;
        }

        public async Task<RoutinePlannerConfirmationDto> Handle(
            ConfirmRoutineProposalCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var proposal = await _proposalGenerator.GenerateForUserAsync(
                userId,
                cancellationToken);

            if (proposal.ProposalId != request.ProposalId ||
                !string.Equals(
                    proposal.CanonicalHash,
                    request.ProposalHash?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "La propuesta cambió y debe regenerarse antes de " +
                    "confirmarla.");
            }

            if (proposal.Feasibility ==
                RoutineProposalFeasibility.NotFeasible)
            {
                throw new ConflictException(
                    "La propuesta no es factible y no puede confirmarse.");
            }

            var routine = BuildRoutine(userId, proposal);
            var persistenceResult = await _routineRepository
                .ConfirmPlannerProposalAsync(
                    routine,
                    cancellationToken);

            return new RoutinePlannerConfirmationDto(
                persistenceResult.RoutineId,
                proposal.ProposalId,
                persistenceResult.IsActive,
                persistenceResult.CreatedNow);
        }

        private static Rutina BuildRoutine(
            string userId,
            RoutinePlannerProposal proposal)
        {
            var routine = new Rutina(
                userId,
                RoutineName,
                null,
                proposal.ProposalId,
                proposal.CanonicalHash);

            foreach (var proposalDay in proposal.Days.OrderBy(day => day.Order))
            {
                var day = routine.AgregarDia(
                    BuildDayName(
                        proposalDay.Focus,
                        proposalDay.Variant),
                    null,
                    proposalDay.Order,
                    proposalDay.ScheduledDay);

                foreach (var exercise in proposalDay.Exercises
                    .OrderBy(item => item.Order))
                {
                    day.AgregarEjercicio(
                        exercise.ExerciseId,
                        exercise.Sets,
                        exercise.MinReps,
                        exercise.MaxReps,
                        exercise.MinRir,
                        exercise.MaxRir,
                        exercise.RestSeconds,
                        exercise.Order,
                        null);
                }
            }

            return routine;
        }

        private static string BuildDayName(
            RoutineSessionFocus focus,
            RoutineSessionVariant variant)
        {
            var focusName = focus switch
            {
                RoutineSessionFocus.FullBody => "Cuerpo completo",
                RoutineSessionFocus.Upper => "Tren superior",
                RoutineSessionFocus.Lower => "Tren inferior",
                RoutineSessionFocus.Push => "Empuje",
                RoutineSessionFocus.Pull => "Tracción",
                RoutineSessionFocus.Legs => "Piernas",
                RoutineSessionFocus.CoreCalves => "Core y gemelos",
                _ => throw new ArgumentOutOfRangeException(nameof(focus))
            };

            return variant == RoutineSessionVariant.None
                ? focusName
                : $"{focusName} {variant}";
        }
    }
}
