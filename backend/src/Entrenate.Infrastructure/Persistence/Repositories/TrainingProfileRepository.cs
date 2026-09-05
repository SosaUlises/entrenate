using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence.Repositories
{
    public class TrainingProfileRepository
    : ITrainingProfileRepository
    {
        private readonly EntrenateDbContext _context;

        public TrainingProfileRepository(
            EntrenateDbContext context)
        {
            _context = context;
        }

        public async Task<PerfilEntrenamiento?> GetByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context
                .PerfilesEntrenamiento
                .Include(x => x.DiasPreferidos)
                .Include(x => x.Equipamientos)
                .FirstOrDefaultAsync(
                    x => x.UsuarioId == userId,
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>>
            GetValidEquipmentIdsAsync(
                IEnumerable<Guid> equipmentIds,
                CancellationToken cancellationToken = default)
        {
            var ids = equipmentIds
                .Distinct()
                .ToList();

            return await _context
                .Equipamientos
                .Where(x =>
                    ids.Contains(x.Id) &&
                    x.Activo)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            PerfilEntrenamiento perfil,
            CancellationToken cancellationToken = default)
        {
            await _context
                .PerfilesEntrenamiento
                .AddAsync(
                    perfil,
                    cancellationToken);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(
                cancellationToken);
        }
    }
}
