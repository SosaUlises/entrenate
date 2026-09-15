using Entrenate.Api.Requests.Routines;
using Entrenate.Application.Routines.Commands.CreateRoutine;
using Entrenate.Application.Routines.Commands.UpdateRoutine;
using Entrenate.Application.Routines.Queries.GetRoutineById;
using Entrenate.Application.Routines.Queries.GetRoutines;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/routines")]
    public class RoutinesController : ControllerBase
    {
        private readonly ISender _sender;

        public RoutinesController(
            ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateRoutineCommand command,
            CancellationToken cancellationToken)
        {
            var id = await _sender.Send(
                command,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var routines = await _sender.Send(
                new GetRoutinesQuery(),
                cancellationToken);

            return Ok(routines);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            Guid id,
            CancellationToken cancellationToken)
        {
            var routine = await _sender.Send(
                new GetRoutineByIdQuery(id),
                cancellationToken);

            return Ok(routine);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            Guid id,
            UpdateRoutineRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(
                new UpdateRoutineCommand(
                    id,
                    request.Nombre,
                    request.Descripcion,
                    request.Dias),
                cancellationToken);

            return NoContent();
        }
    }
}
