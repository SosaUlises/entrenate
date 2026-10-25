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
    }
}
