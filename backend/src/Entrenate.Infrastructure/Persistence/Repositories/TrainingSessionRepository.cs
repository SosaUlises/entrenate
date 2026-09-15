using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence.Repositories
{
    public class TrainingSessionRepository : ITrainingSessionRepository
    {
        private readonly EntrenateDbContext _context;

        public TrainingSessionRepository(EntrenateDbContext context)
        {
            _context = context;
        }

        public Task<bool> HasActiveSessionAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SesionesEntrenamiento.AnyAsync(
                x => x.UsuarioId == userId &&
                    x.Estado == EstadoSesionEntrenamiento.EnCurso,
                cancellationToken);
        }

        public Task<SesionEntrenamiento?> GetActiveByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SesionesEntrenamiento
                .AsNoTracking()
                .Include(x => x.Ejercicios.OrderBy(y => y.Orden))
                    .ThenInclude(x => x.Ejercicio)
                .Include(x => x.Ejercicios.OrderBy(y => y.Orden))
                    .ThenInclude(x => x.Series.OrderBy(y => y.NumeroSerie))
                .Where(x =>
                    x.UsuarioId == userId &&
                    x.Estado == EstadoSesionEntrenamiento.EnCurso)
                .OrderByDescending(x => x.HoraInicio)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(
            Guid sessionId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SesionesEntrenamiento
                .Include(x => x.Ejercicios)
                    .ThenInclude(x => x.Series)
                .FirstOrDefaultAsync(
                    x => x.Id == sessionId &&
                        x.UsuarioId == userId,
                    cancellationToken);
        }

        public async Task AddAsync(
            SesionEntrenamiento session,
            CancellationToken cancellationToken = default)
        {
            await _context.SesionesEntrenamiento.AddAsync(
                session,
                cancellationToken);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
