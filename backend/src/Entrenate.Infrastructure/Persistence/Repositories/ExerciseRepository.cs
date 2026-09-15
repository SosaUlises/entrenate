using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence.Repositories
{
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly EntrenateDbContext _context;

        public ExerciseRepository(
            EntrenateDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Ejercicio>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context
                .Ejercicios
                .AsNoTracking()
                .Include(x => x.Equipamientos)
                .Where(x => x.Activo)
                .OrderBy(x => x.GrupoMuscularPrincipal)
                .ThenBy(x => x.Nombre)
                .ToListAsync(cancellationToken);
        }

        public async Task<Ejercicio?> GetActiveByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context
                .Ejercicios
                .AsNoTracking()
                .Include(x => x.Equipamientos)
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.Activo,
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetValidActiveIdsAsync(
            IEnumerable<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var exerciseIds = ids
                .Distinct()
                .ToList();

            return await _context
                .Ejercicios
                .AsNoTracking()
                .Where(x => exerciseIds.Contains(x.Id) && x.Activo)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
