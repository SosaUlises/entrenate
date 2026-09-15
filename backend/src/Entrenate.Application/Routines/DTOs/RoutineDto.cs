namespace Entrenate.Application.Routines.DTOs
{
    public record RoutineDto(
        Guid Id,
        string Nombre,
        string? Descripcion,
        bool Activa,
        DateTime FechaCreacion,
        DateTime FechaUltimaModificacion,
        IReadOnlyCollection<RoutineDayDto> Dias);
}
