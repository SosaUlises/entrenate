using Entrenate.Application.Routines.Models;

namespace Entrenate.Api.Requests.Routines
{
    public record UpdateRoutineRequest(
        string Nombre,
        string? Descripcion,
        List<RoutineDayInput> Dias);
}
