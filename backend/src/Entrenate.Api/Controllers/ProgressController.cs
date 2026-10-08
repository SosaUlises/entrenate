using Entrenate.Application.Progress.Queries.GetExerciseProgress;
using Entrenate.Application.Progress.Queries.GetExerciseProgressHub;
using Entrenate.Application.Progress.Queries.GetExerciseProgressTrend;
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

        [HttpGet("exercises")]
        public async Task<IActionResult> GetExerciseProgressHub(
            CancellationToken cancellationToken)
        {
            var progress = await _sender.Send(
                new GetExerciseProgressHubQuery(),
                cancellationToken);

            return Ok(progress);
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

        [HttpGet("exercises/{exerciseId:guid}/trend")]
        public async Task<IActionResult> GetExerciseProgressTrend(
            Guid exerciseId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] string? timeZone,
            CancellationToken cancellationToken)
        {
            var trend = await _sender.Send(
                new GetExerciseProgressTrendQuery(
                    exerciseId,
                    from,
                    to,
                    timeZone),
                cancellationToken);

            return Ok(trend);
        }
    }
}
