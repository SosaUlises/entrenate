using Entrenate.Api.Requests.Coach;
using Entrenate.Application.Coach.RoutinePlanner.Commands.ConfirmRoutineProposal;
using Entrenate.Application.Coach.RoutinePlanner.Conversation.Commands.AskRoutinePlannerConversation;
using Entrenate.Application.Coach.RoutinePlanner.Queries.GenerateRoutineProposal;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/coach/routine-planner")]
    public sealed class CoachRoutinePlannerController : ControllerBase
    {
        private readonly ISender _sender;

        public CoachRoutinePlannerController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("proposals")]
        public async Task<IActionResult> GenerateProposal(
            CancellationToken cancellationToken)
        {
            var proposal = await _sender.Send(
                new GenerateRoutineProposalQuery(),
                cancellationToken);

            return Ok(proposal);
        }

        [HttpPost("proposals/{proposalId:guid}/confirm")]
        public async Task<IActionResult> ConfirmProposal(
            Guid proposalId,
            ConfirmRoutineProposalRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(
                new ConfirmRoutineProposalCommand(
                    proposalId,
                    request.ProposalHash),
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("proposals/{proposalId:guid}/conversation")]
        public async Task<IActionResult> AskProposalConversation(
            Guid proposalId,
            AskRoutinePlannerConversationRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new AskRoutinePlannerConversationCommand(
                    proposalId,
                    request.ProposalHash,
                    request.Question),
                cancellationToken);

            return Ok(response);
        }
    }
}
