using Entrenate.Api.Requests.Coach;
using Entrenate.Application.Coach.Progress.Conversation.Commands.AskProgressConversation;
using Entrenate.Application.Coach.Progress.Queries.GetCoachProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/coach/progress")]
    public class CoachProgressController : ControllerBase
    {
        private readonly ISender _sender;

        public CoachProgressController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("exercises/{exerciseId:guid}")]
        public async Task<IActionResult> GetExerciseProgress(
            Guid exerciseId,
            CancellationToken cancellationToken)
        {
            var progress = await _sender.Send(
                new GetCoachProgressQuery(exerciseId),
                cancellationToken);

            return Ok(progress);
        }

        [HttpPost("exercises/{exerciseId:guid}/conversation")]
        public async Task<IActionResult> AskProgressConversation(
            Guid exerciseId,
            AskProgressConversationRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _sender.Send(
                new AskProgressConversationCommand(
                    exerciseId,
                    request.Question),
                cancellationToken);

            return Ok(response);
        }
    }
}
