using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence.Repositories
{
    public class RoutineRepository : IRoutineRepository
    {
        private readonly EntrenateDbContext _context;

        public RoutineRepository(
            EntrenateDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Rutina>> GetByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context
                .Rutinas
                .AsNoTracking()
                .Include(x => x.Dias)
                .Where(x => x.UsuarioId == userId)
                .OrderByDescending(x => x.FechaUltimaModificacion)
                .ThenByDescending(x => x.FechaCreacion)
                .ToListAsync(cancellationToken);
        }

        public async Task<Rutina?> GetDetailByIdAndUserIdAsync(
            Guid id,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context
                .Rutinas
                .AsNoTracking()
                .Include(x => x.Dias)
                    .ThenInclude(x => x.Ejercicios)
                        .ThenInclude(x => x.Ejercicio)
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UsuarioId == userId,
                    cancellationToken);
        }

        public async Task<Rutina?> GetForUpdateByIdAndUserIdAsync(
            Guid id,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context
                .Rutinas
                .Include(x => x.Dias)
                    .ThenInclude(x => x.Ejercicios)
                .FirstOrDefaultAsync(
                    x => x.Id == id && x.UsuarioId == userId,
                    cancellationToken);
        }

        public async Task AddAsync(
            Rutina rutina,
            CancellationToken cancellationToken = default)
        {
            await _context.Rutinas.AddAsync(
                rutina,
                cancellationToken);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
