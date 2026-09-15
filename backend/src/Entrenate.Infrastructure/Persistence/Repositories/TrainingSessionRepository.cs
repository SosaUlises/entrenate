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
