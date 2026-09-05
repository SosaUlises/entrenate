using Entrenate.Application.Equipment.Queries.GetEquipment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/equipment")]
    public class EquipmentController : ControllerBase
    {
        private readonly ISender _sender;

        public EquipmentController(
            ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken cancellationToken)
        {
            var equipment = await _sender.Send(
                new GetEquipmentQuery(),
                cancellationToken);

            return Ok(equipment);
        }
    }
}
