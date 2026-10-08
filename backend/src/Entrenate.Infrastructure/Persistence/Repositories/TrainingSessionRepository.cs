using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;
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
