using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Substitution;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using MediatR;

namespace Entrenate.Application.Coach.AdaptiveTraining.Commands.ConfirmSessionAdaptationProposal
{
    public sealed class ConfirmSessionAdaptationProposalCommandHandler
        : IRequestHandler<ConfirmSessionAdaptationProposalCommand, SessionAdaptationConfirmationDto>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly SessionAdaptationProposalGenerator _proposalGenerator;
        private readonly ITrainingSessionRepository _sessionRepository;

        public ConfirmSessionAdaptationProposalCommandHandler(
            ICurrentUserService currentUserService,
            SessionAdaptationProposalGenerator proposalGenerator,
            ITrainingSessionRepository sessionRepository)
        {
            _currentUserService = currentUserService;
            _proposalGenerator = proposalGenerator;
            _sessionRepository = sessionRepository;
        }

        public async Task<SessionAdaptationConfirmationDto> Handle(
            ConfirmSessionAdaptationProposalCommand request,
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
                request.RoutineDayId,
                request.AvailableMinutes,
                request.AvailableEquipmentIds,
                request.ExcludedExerciseIds,
                request.EffortPreference,
                cancellationToken);

            var proposalIdString = proposal.AdaptationProposalId.ToString();

            if (!string.Equals(proposalIdString, request.ProposalId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    proposal.ProposalHash,
                    request.ProposalHash?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "La propuesta cambió y debe regenerarse antes de continuar.");
            }

            if (proposal.Feasibility == SessionAdaptationFeasibility.NotFeasible)
            {
                throw new ConflictException(
                    "La propuesta no es factible y no puede confirmarse.");
            }

            var adaptedExercises = proposal.AdaptedSession.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new EjercicioSesion(
                    e.ExerciseId,
                    e.Order,
                    e.Prescription.Sets,
                    e.Prescription.MinReps,
                    e.Prescription.MaxReps,
                    e.Prescription.MinRir,
                    e.Prescription.MaxRir,
                    e.Prescription.RestSeconds,
                    e.Prescription.Notes))
                .ToList();

            var session = new SesionEntrenamiento(
                userId,
                request.RoutineDayId,
                DateTime.UtcNow,
                adaptedExercises);

            var factibilidadDomain = proposal.Feasibility switch
            {
                SessionAdaptationFeasibility.Feasible => FactibilidadAdaptacionSesion.Factible,
                SessionAdaptationFeasibility.FeasibleWithChanges => FactibilidadAdaptacionSesion.FactibleConCambios,
                _ => FactibilidadAdaptacionSesion.NoFactible
            };

            var effortDomain = proposal.Context.EffortPreference switch
            {
                SessionEffortPreference.Lighter => PreferenciaEsfuerzoSesion.Liviana,
                _ => PreferenciaEsfuerzoSesion.Normal
            };

            var adaptation = new SesionAdaptacion(
                session.Id,
                userId,
                proposalIdString,
                proposal.ProposalHash,
                proposal.PolicyVersion,
                factibilidadDomain,
                effortDomain,
                proposal.Context.AvailableMinutes,
                DateTime.UtcNow,
                proposal.Context.AvailableEquipmentIds,
                proposal.Context.ExcludedExerciseIds);

            foreach (var change in proposal.Changes)
            {
                var tipoCambio = change.ChangeType switch
                {
                    SessionAdaptationChangeType.ExerciseSubstituted => TipoCambioAdaptacionSesion.EjercicioSustituido,
                    SessionAdaptationChangeType.OptionalAccessoryRemovedNoSubstitute => TipoCambioAdaptacionSesion.AccesorioEliminadoSinSustituto,
                    SessionAdaptationChangeType.RequiredExerciseUnavailable => TipoCambioAdaptacionSesion.EjercicioRequeridoNoDisponible,
                    SessionAdaptationChangeType.ExerciseSetsReducedForTime => TipoCambioAdaptacionSesion.SeriesReducidasPorTiempo,
                    SessionAdaptationChangeType.OptionalAccessoryRemovedForTime => TipoCambioAdaptacionSesion.AccesorioEliminadoPorTiempo,
                    SessionAdaptationChangeType.RirIncreased => TipoCambioAdaptacionSesion.RirAumentado,
                    SessionAdaptationChangeType.OptionalAccessorySetsReducedForLighter => TipoCambioAdaptacionSesion.SeriesReducidasPorModoLiviano,
                    _ => throw new ArgumentOutOfRangeException(nameof(change.ChangeType))
                };

                MotivoSustitucionAdaptacion? motivo = null;
                if (change.SubstitutionReason.HasValue)
                {
                    motivo = MotivoSustitucionAdaptacion.Ninguno;
                    if (change.SubstitutionReason.Value.HasFlag(AdaptiveSubstitutionReason.EquipmentUnavailable))
                    {
                        motivo |= MotivoSustitucionAdaptacion.EquipamientoNoDisponible;
                    }
                    if (change.SubstitutionReason.Value.HasFlag(AdaptiveSubstitutionReason.UserExcludedExercise))
                    {
                        motivo |= MotivoSustitucionAdaptacion.EjercicioExcluidoPorUsuario;
                    }
                }

                adaptation.AgregarCambio(
                    tipoCambio,
                    change.SourceRoutineExerciseId,
                    change.ExerciseId,
                    change.ExerciseCode,
                    change.SubstitutedExerciseId,
                    change.SubstitutedExerciseCode,
                    change.OriginalSets,
                    change.AdaptedSets,
                    change.OriginalMinRir,
                    change.OriginalMaxRir,
                    change.AdaptedMinRir,
                    change.AdaptedMaxRir,
                    motivo);
            }

            var persistenceResult = await _sessionRepository.ConfirmAdaptiveSessionAsync(
                session,
                adaptation,
                cancellationToken);

            return new SessionAdaptationConfirmationDto(
                persistenceResult.SessionId,
                persistenceResult.ProposalId,
                persistenceResult.CreatedNow);
        }
    }
}
