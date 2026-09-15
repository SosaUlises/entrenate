using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Routines.DTOs;
using MediatR;

namespace Entrenate.Application.Routines.Queries.GetRoutines
{
    public class GetRoutinesQueryHandler
    : IRequestHandler<
        GetRoutinesQuery,
        IReadOnlyCollection<RoutineSummaryDto>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IRoutineRepository _routineRepository;

        public GetRoutinesQueryHandler(
            ICurrentUserService currentUserService,
            IRoutineRepository routineRepository)
        {
            _currentUserService = currentUserService;
            _routineRepository = routineRepository;
        }

        public async Task<IReadOnlyCollection<RoutineSummaryDto>> Handle(
            GetRoutinesQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");
            }

            var routines = await _routineRepository.GetByUserIdAsync(
                userId,
                cancellationToken);

            return routines
                .Select(x => new RoutineSummaryDto(
                    x.Id,
                    x.Nombre,
                    x.Descripcion,
                    x.Activa,
                    x.Dias.Count,
                    x.FechaCreacion,
                    x.FechaUltimaModificacion))
                .ToList();
        }
    }
}
