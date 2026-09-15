using Entrenate.Application.Exercises.Queries.GetExerciseById;
using Entrenate.Application.Exercises.Queries.GetExercises;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/exercises")]
    public class ExercisesController : ControllerBase
    {
        private readonly ISender _sender;

        public ExercisesController(
            ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var exercises = await _sender.Send(
                new GetExercisesQuery(),
                cancellationToken);

            return Ok(exercises);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var exercise = await _sender.Send(
                new GetExerciseByIdQuery(id),
                cancellationToken);

            return Ok(exercise);
        }
    }
}
