using Entrenate.Api.Requests.TrainingSessions;
using Entrenate.Application.TrainingSessions.Commands.StartTrainingSession;
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
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

        [HttpPut(
            "{sessionId:guid}/exercises/{exerciseSessionId:guid}/" +
            "sets/{setNumber:int}")]
        public async Task<IActionResult> UpsertSet(
            Guid sessionId,
            Guid exerciseSessionId,
            int setNumber,
            UpsertTrainingSetRequest request,
            CancellationToken cancellationToken)
        {
            var trainingSet = await _sender.Send(
                new UpsertTrainingSetCommand(
                    sessionId,
                    exerciseSessionId,
                    setNumber,
                    request.Peso,
                    request.Repeticiones,
                    request.Rir),
                cancellationToken);

            return Ok(trainingSet);
        }
    }
}
