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
    }
}
