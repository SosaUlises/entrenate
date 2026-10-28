using Entrenate.Api.Requests.Coach;
using Entrenate.Application.Coach.AdaptiveTraining.Commands.ConfirmSessionAdaptationProposal;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Queries.GenerateSessionAdaptationProposal;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/coach/adaptive-training")]
    public sealed class CoachAdaptiveTrainingController : ControllerBase
    {
        private readonly ISender _sender;

        public CoachAdaptiveTrainingController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("proposals")]
        public async Task<IActionResult> GenerateProposal(
            [FromBody] GenerateSessionAdaptationProposalRequest request,
            CancellationToken cancellationToken)
        {
            var effort = ParseEffortPreference(request.EffortPreference);

            var proposal = await _sender.Send(
                new GenerateSessionAdaptationProposalQuery(
                    request.RoutineDayId,
                    request.AvailableMinutes,
                    request.AvailableEquipmentIds,
                    request.ExcludedExerciseIds,
                    effort),
                cancellationToken);

            return Ok(proposal);
        }

        [HttpPost("proposals/{proposalId}/confirm")]
        public async Task<IActionResult> ConfirmProposal(
            string proposalId,
            [FromBody] ConfirmSessionAdaptationProposalRequest request,
            CancellationToken cancellationToken)
        {
            var effort = ParseEffortPreference(request.EffortPreference);

            var result = await _sender.Send(
                new ConfirmSessionAdaptationProposalCommand(
                    proposalId,
                    request.ProposalHash,
                    request.RoutineDayId,
                    request.AvailableMinutes,
                    request.AvailableEquipmentIds,
                    request.ExcludedExerciseIds,
                    effort),
                cancellationToken);

            return Ok(result);
        }

        private static SessionEffortPreference ParseEffortPreference(string? preference)
        {
            if (string.IsNullOrWhiteSpace(preference))
            {
                return SessionEffortPreference.Normal;
            }

            return string.Equals(preference.Trim(), "Lighter", StringComparison.OrdinalIgnoreCase)
                ? SessionEffortPreference.Lighter
                : SessionEffortPreference.Normal;
        }
    }
}

