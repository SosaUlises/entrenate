using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Coach.RoutinePlanner.Confirmation;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
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

        public async Task<DiaRutina?> GetDayByIdAndUserIdAsync(
            Guid dayId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context
                .DiasRutina
                .AsNoTracking()
                .Include(x => x.Rutina)
                .Include(x => x.Ejercicios)
                    .ThenInclude(x => x.Ejercicio)
                .FirstOrDefaultAsync(
                    x => x.Id == dayId &&
                        x.Rutina.UsuarioId == userId,
                    cancellationToken);
        }

        public Task<bool> HasActiveSessionAsync(
            Guid routineId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SesionesEntrenamiento
                .AsNoTracking()
                .AnyAsync(
                    x => x.UsuarioId == userId &&
                        x.Estado == EstadoSesionEntrenamiento.EnCurso &&
                        x.DiaRutinaId.HasValue &&
                        x.DiaRutina!.RutinaId == routineId,
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

        public async Task<RoutineProposalPersistenceResult>
            ConfirmPlannerProposalAsync(
                Rutina routine,
                CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(routine);

            if (!routine.PlannerProposalId.HasValue ||
                string.IsNullOrWhiteSpace(routine.PlannerProposalHash))
            {
                throw new ArgumentException(
                    "La rutina debe identificar la propuesta confirmada.",
                    nameof(routine));
            }

            try
            {
                await using var transaction = await _context.Database
                    .BeginTransactionAsync(cancellationToken);
                var existing = await FindByProposalAsync(
                    routine.UsuarioId,
                    routine.PlannerProposalId.Value,
                    cancellationToken);

                if (existing is not null)
                {
                    return ResolveExisting(
                        existing,
                        routine.PlannerProposalHash);
                }

                var activeRoutines = await _context.Rutinas
                    .Where(item =>
                        item.UsuarioId == routine.UsuarioId &&
                        item.Activa)
                    .ToListAsync(cancellationToken);

                foreach (var activeRoutine in activeRoutines)
                {
                    activeRoutine.Desactivar();
                }

                await _context.Rutinas.AddAsync(
                    routine,
                    cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new RoutineProposalPersistenceResult(
                    routine.Id,
                    routine.Activa,
                    true);
            }
            catch (DbUpdateException exception) when (
                IsProposalUniquenessViolation(exception))
            {
                _context.ChangeTracker.Clear();

                var existing = await FindByProposalAsync(
                    routine.UsuarioId,
                    routine.PlannerProposalId.Value,
                    cancellationToken);

                if (existing is null)
                {
                    throw new ConflictException(
                        "No se pudo confirmar la propuesta de rutina.");
                }

                return ResolveExisting(
                    existing,
                    routine.PlannerProposalHash);
            }
        }

        private Task<Rutina?> FindByProposalAsync(
            string userId,
            Guid proposalId,
            CancellationToken cancellationToken)
        {
            return _context.Rutinas
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.UsuarioId == userId &&
                        item.PlannerProposalId == proposalId,
                    cancellationToken);
        }

        private static RoutineProposalPersistenceResult ResolveExisting(
            Rutina existing,
            string expectedHash)
        {
            if (!string.Equals(
                existing.PlannerProposalHash,
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "La propuesta ya fue confirmada con otro contenido.");
            }

            return new RoutineProposalPersistenceResult(
                existing.Id,
                existing.Activa,
                false);
        }

        private static bool IsProposalUniquenessViolation(
            DbUpdateException exception)
        {
            return exception.InnerException is Npgsql.PostgresException
            {
                SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_Rutinas_UsuarioId_PlannerProposalId"
            };
        }

        public void Remove(Rutina rutina)
        {
            _context.Rutinas.Remove(rutina);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
