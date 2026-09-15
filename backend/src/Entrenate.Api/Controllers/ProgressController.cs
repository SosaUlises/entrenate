using Entrenate.Application.Progress.Queries.GetExerciseProgress;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/progress")]
    public class ProgressController : ControllerBase
    {
        private readonly ISender _sender;

        public ProgressController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("exercises/{exerciseId:guid}")]
        public async Task<IActionResult> GetExerciseProgress(
            Guid exerciseId,
            CancellationToken cancellationToken)
        {
            var progress = await _sender.Send(
                new GetExerciseProgressQuery(exerciseId),
                cancellationToken);

            return Ok(progress);
        }
    }
}
