namespace Entrenate.Application.Routines.DTOs
{
    public record RoutineSummaryDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        bool Activa,
        int CantidadDias,
        DateTime FechaCreacion,
        DateTime FechaUltimaModificacion);
}
