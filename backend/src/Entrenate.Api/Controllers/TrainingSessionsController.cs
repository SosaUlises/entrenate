using Entrenate.Application.TrainingSessions.Commands.StartTrainingSession;
using Entrenate.Application.TrainingSessions.Queries.GetActiveTrainingSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/training-sessions")]
    public class TrainingSessionsController : ControllerBase
    {
        private readonly ISender _sender;

        public TrainingSessionsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Start(
            StartTrainingSessionCommand command,
            CancellationToken cancellationToken)
        {
            var session = await _sender.Send(
                command,
                cancellationToken);

            return Created(
                $"api/training-sessions/{session.Id}",
                session);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive(
            CancellationToken cancellationToken)
        {
            var session = await _sender.Send(
                new GetActiveTrainingSessionQuery(),
                cancellationToken);

            return Ok(session);
        }
    }
}
