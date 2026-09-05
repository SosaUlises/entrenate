using Entrenate.Application.TrainingProfiles.Commands.CreateTrainingProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Entrenate.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/training-profile")]
    public class TrainingProfilesController : ControllerBase
    {
        private readonly ISender _sender;

        public TrainingProfilesController(
            ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateTrainingProfileCommand command,
            CancellationToken cancellationToken)
        {
            var profileId = await _sender.Send(
                command,
                cancellationToken);

            return Created(
                $"api/training-profile/{profileId}",
                new
                {
                    id = profileId
                });
        }
    }
}
