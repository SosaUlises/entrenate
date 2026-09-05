using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence.Repositories
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly EntrenateDbContext _context;

        public EquipmentRepository(
            EntrenateDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Equipamiento>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context
                .Equipamientos
                .AsNoTracking()
                .Where(x => x.Activo)
                .OrderBy(x => x.Categoria)
                .ThenBy(x => x.Nombre)
                .ToListAsync(cancellationToken);
        }
    }
}
