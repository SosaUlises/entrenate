using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Entrenate.Infrastructure.Persistence.Repositories
{
    public class TrainingSessionRepository : ITrainingSessionRepository
    {
        private const int ProgressCoachRecentSessionWindow = 3;

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

        public async Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>>
            GetHistoryByUserIdAsync(
                string userId,
                CancellationToken cancellationToken = default)
        {
            return await _context.SesionesEntrenamiento
                .AsNoTracking()
                .Where(x =>
                    x.UsuarioId == userId &&
                    (x.Estado == EstadoSesionEntrenamiento.Completada ||
                        x.Estado == EstadoSesionEntrenamiento.Cancelada))
                .OrderByDescending(x => x.HoraInicio)
                .ThenByDescending(x => x.Id)
                .Select(x => new TrainingSessionHistorySummaryDto(
                    x.Id,
                    x.DiaRutinaId,
                    x.Fecha,
                    x.HoraInicio,
                    x.HoraFin,
                    x.Estado,
                    x.Ejercicios.Count,
                    x.Ejercicios
                        .SelectMany(y => y.Series)
                        .Count(y => y.Completada)))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<DateTime>>
            GetCompletedSessionStartTimesByUserIdAsync(
                string userId,
                DateTime fromUtc,
                DateTime toUtcExclusive,
                CancellationToken cancellationToken = default)
        {
            return await _context.SesionesEntrenamiento
                .AsNoTracking()
                .Where(x =>
                    x.UsuarioId == userId &&
                    x.Estado == EstadoSesionEntrenamiento.Completada &&
                    x.HoraInicio >= fromUtc &&
                    x.HoraInicio < toUtcExclusive)
                .OrderBy(x => x.HoraInicio)
                .Select(x => x.HoraInicio)
                .ToListAsync(cancellationToken);
        }

        public Task<SesionEntrenamiento?> GetByIdAndUserIdAsNoTrackingAsync(
            Guid sessionId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SesionesEntrenamiento
                .AsNoTracking()
                .Include(x => x.Ejercicios.OrderBy(y => y.Orden))
                    .ThenInclude(x => x.Ejercicio)
                .Include(x => x.Ejercicios.OrderBy(y => y.Orden))
                    .ThenInclude(x => x.Series.OrderBy(y => y.NumeroSerie))
                .FirstOrDefaultAsync(
                    x => x.Id == sessionId &&
                        x.UsuarioId == userId,
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<ExerciseProgressEntryDto>>
            GetCompletedExerciseProgressByUserIdAsync(
                Guid exerciseId,
                string userId,
                CancellationToken cancellationToken = default)
        {
            return await _context.SesionesEntrenamiento
                .AsNoTracking()
                .Where(x =>
                    x.UsuarioId == userId &&
                    x.Estado == EstadoSesionEntrenamiento.Completada &&
                    x.Ejercicios.Any(y =>
                        y.EjercicioId == exerciseId &&
                        y.Series.Any(z => z.Completada)))
                .OrderBy(x => x.HoraInicio)
                .ThenBy(x => x.Id)
                .Select(x => new ExerciseProgressEntryDto(
                    x.Id,
                    x.Fecha,
                    x.HoraInicio,
                    x.Ejercicios
                        .Where(y => y.EjercicioId == exerciseId)
                        .SelectMany(y => y.Series)
                        .Where(y => y.Completada)
                        .OrderBy(y => y.NumeroSerie)
                        .Select(y => new TrainingSetDto(
                            y.Id,
                            y.NumeroSerie,
                            y.Peso,
                            y.Repeticiones,
                            y.Rir,
                            y.Completada,
                            y.FechaHoraRegistro))
                        .ToList()))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<ExerciseProgressEntryDto>>
            GetCompletedExerciseProgressTrendByUserIdAsync(
                Guid exerciseId,
                string userId,
                DateTime? fromUtc,
                DateTime? toUtcExclusive,
                CancellationToken cancellationToken = default)
        {
            var sessions = _context.SesionesEntrenamiento
                .AsNoTracking()
                .Where(x =>
                    x.UsuarioId == userId &&
                    x.Estado == EstadoSesionEntrenamiento.Completada &&
                    x.Ejercicios.Any(y =>
                        y.EjercicioId == exerciseId &&
                        y.Series.Any(z => z.Completada)));

            if (fromUtc.HasValue)
            {
                sessions = sessions.Where(x => x.HoraInicio >= fromUtc.Value);
            }

            if (toUtcExclusive.HasValue)
            {
                sessions = sessions.Where(x => x.HoraInicio < toUtcExclusive.Value);
            }

            return await sessions
                .OrderBy(x => x.HoraInicio)
                .ThenBy(x => x.Id)
                .Select(x => new ExerciseProgressEntryDto(
                    x.Id,
                    x.Fecha,
                    x.HoraInicio,
                    x.Ejercicios
                        .Where(y => y.EjercicioId == exerciseId)
                        .SelectMany(y => y.Series)
                        .Where(y => y.Completada)
                        .OrderBy(y => y.NumeroSerie)
                        .ThenBy(y => y.Id)
                        .Select(y => new TrainingSetDto(
                            y.Id,
                            y.NumeroSerie,
                            y.Peso,
                            y.Repeticiones,
                            y.Rir,
                            y.Completada,
                            y.FechaHoraRegistro))
                        .ToList()))
                .ToListAsync(cancellationToken);
        }

        public Task<bool> HasCompletedExerciseProgressByUserIdAsync(
            Guid exerciseId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return _context.SesionesEntrenamiento
                .AsNoTracking()
                .AnyAsync(
                    x => x.UsuarioId == userId &&
                        x.Estado == EstadoSesionEntrenamiento.Completada &&
                        x.Ejercicios.Any(y =>
                            y.EjercicioId == exerciseId &&
                            y.Series.Any(z => z.Completada)),
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<ExerciseProgressHubSourceDto>>
            GetCompletedExerciseProgressHubByUserIdAsync(
                string userId,
                CancellationToken cancellationToken = default)
        {
            return await _context.EjerciciosSesion
                .AsNoTracking()
                .Where(x =>
                    x.SesionEntrenamiento.UsuarioId == userId &&
                    x.SesionEntrenamiento.Estado ==
                        EstadoSesionEntrenamiento.Completada &&
                    x.Series.Any(y => y.Completada))
                .OrderBy(x => x.SesionEntrenamiento.HoraInicio)
                .ThenBy(x => x.SesionEntrenamientoId)
                .ThenBy(x => x.EjercicioId)
                .Select(x => new ExerciseProgressHubSourceDto(
                    x.EjercicioId,
                    x.Ejercicio.Nombre,
                    x.Ejercicio.GrupoMuscularPrincipal,
                    x.SesionEntrenamientoId,
                    x.SesionEntrenamiento.Fecha,
                    x.SesionEntrenamiento.HoraInicio,
                    x.Series
                        .Where(y => y.Completada)
                        .OrderBy(y => y.NumeroSerie)
                        .ThenBy(y => y.Id)
                        .Select(y => new TrainingSetDto(
                            y.Id,
                            y.NumeroSerie,
                            y.Peso,
                            y.Repeticiones,
                            y.Rir,
                            y.Completada,
                            y.FechaHoraRegistro))
                        .ToList()))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<ProgressExposure>>
            GetRecentCompletedExerciseExposuresByUserIdAsync(
                Guid exerciseId,
                string userId,
                CancellationToken cancellationToken = default)
        {
            var recentSessionIds = _context.SesionesEntrenamiento
                .AsNoTracking()
                .Where(x =>
                    x.UsuarioId == userId &&
                    x.Estado == EstadoSesionEntrenamiento.Completada &&
                    x.Ejercicios.Any(y =>
                        y.EjercicioId == exerciseId &&
                        y.Series.Any(z => z.Completada)))
                .OrderByDescending(x => x.HoraInicio)
                .ThenByDescending(x => x.Id)
                .Select(x => x.Id)
                .Take(ProgressCoachRecentSessionWindow);

            return await _context.EjerciciosSesion
                .AsNoTracking()
                .Where(x =>
                    recentSessionIds.Contains(x.SesionEntrenamientoId) &&
                    x.EjercicioId == exerciseId &&
                    x.Series.Any(y => y.Completada))
                .OrderByDescending(x => x.SesionEntrenamiento.HoraInicio)
                .ThenByDescending(x => x.SesionEntrenamientoId)
                .ThenBy(x => x.Orden)
                .ThenBy(x => x.Id)
                .Select(x => new ProgressExposure(
                    x.Id,
                    x.SesionEntrenamientoId,
                    x.EjercicioId,
                    x.SesionEntrenamiento.HoraInicio,
                    new ProgressPrescriptionSnapshot(
                        x.SeriesObjetivo,
                        x.RepeticionesMinimasObjetivo,
                        x.RepeticionesMaximasObjetivo,
                        x.RirObjetivoMinimo,
                        x.RirObjetivoMaximo,
                        x.DescansoObjetivoSegundos),
                    x.Series
                        .Where(y => y.Completada)
                        .OrderBy(y => y.NumeroSerie)
                        .ThenBy(y => y.Id)
                        .Select(y => new ProgressCompletedSet(
                            y.NumeroSerie,
                            y.Peso,
                            y.Repeticiones,
                            y.Rir))
                        .ToList()))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(
            SesionEntrenamiento session,
            CancellationToken cancellationToken = default)
        {
            await _context.SesionesEntrenamiento.AddAsync(
                session,
                cancellationToken);
        }


        public async Task<AdaptiveSessionPersistenceResult> ConfirmAdaptiveSessionAsync(
            SesionEntrenamiento session,
            SesionAdaptacion adaptation,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(adaptation);

            try
            {
                await using var transaction = await _context.Database
                    .BeginTransactionAsync(cancellationToken);

                var existing = await _context.SesionesAdaptacion
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        a => a.UsuarioId == adaptation.UsuarioId &&
                             a.AdaptationProposalId == adaptation.AdaptationProposalId,
                        cancellationToken);

                if (existing is not null)
                {
                    return ResolveExistingAdaptation(
                        existing,
                        adaptation.ProposalHash);
                }

                var hasActiveSession = await _context.SesionesEntrenamiento
                    .AnyAsync(
                        s => s.UsuarioId == session.UsuarioId &&
                             s.Estado == EstadoSesionEntrenamiento.EnCurso,
                        cancellationToken);

                if (hasActiveSession)
                {
                    throw new ConflictException(
                        "Ya existe una sesión de entrenamiento en curso.");
                }

                session.AsignarAdaptacion(adaptation);

                await _context.SesionesEntrenamiento.AddAsync(
                    session,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new AdaptiveSessionPersistenceResult(
                    session.Id,
                    adaptation.AdaptationProposalId,
                    true);
            }
            catch (DbUpdateException exception) when (
                IsAdaptationProposalUniquenessViolation(exception))
            {
                _context.ChangeTracker.Clear();

                var existing = await _context.SesionesAdaptacion
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        a => a.UsuarioId == adaptation.UsuarioId &&
                             a.AdaptationProposalId == adaptation.AdaptationProposalId,
                        cancellationToken);

                if (existing is null)
                {
                    throw new ConflictException(
                        "No se pudo confirmar la adaptación de sesión.");
                }

                return ResolveExistingAdaptation(
                    existing,
                    adaptation.ProposalHash);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is Npgsql.PostgresException
                {
                    SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "IX_SesionesEntrenamiento_UsuarioId_Active"
                })
            {
                throw new ConflictException(
                    "Ya existe una sesión de entrenamiento en curso.");
            }
        }

        private static AdaptiveSessionPersistenceResult ResolveExistingAdaptation(
            SesionAdaptacion existing,
            string expectedHash)
        {
            if (!string.Equals(
                existing.ProposalHash,
                expectedHash,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "La propuesta ya fue confirmada con otro contenido.");
            }

            return new AdaptiveSessionPersistenceResult(
                existing.SesionEntrenamientoId,
                existing.AdaptationProposalId,
                false);
        }

        private static bool IsAdaptationProposalUniquenessViolation(
            DbUpdateException exception)
        {
            return exception.InnerException is Npgsql.PostgresException
            {
                SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_SesionesAdaptacion_UsuarioId_AdaptationProposalId"
            };
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is Npgsql.PostgresException
                {
                    SqlState: Npgsql.PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "IX_SesionesEntrenamiento_UsuarioId_Active"
                })
            {
                throw new Entrenate.Application.Common.Exceptions.ConflictException(
                    "Ya existe una sesión de entrenamiento en curso.");
            }
        }
    }
}
