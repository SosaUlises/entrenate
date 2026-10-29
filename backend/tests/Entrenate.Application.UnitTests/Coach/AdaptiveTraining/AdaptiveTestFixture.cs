using System.Reflection;
using Entrenate.Application.Coach.AdaptiveTraining.Commands.ConfirmSessionAdaptationProposal;
using Entrenate.Application.Coach.AdaptiveTraining.DTOs;
using Entrenate.Application.Coach.AdaptiveTraining.Proposal;
using Entrenate.Application.Coach.AdaptiveTraining.Queries.GenerateSessionAdaptationProposal;
using Entrenate.Application.Coach.Progress.Analysis;
using Entrenate.Application.Coach.RoutinePlanner.Confirmation;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Progress.DTOs;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;

namespace Entrenate.Application.UnitTests.Coach.AdaptiveTraining
{
    public sealed class AdaptiveTestFixture
    {
        public const string UserId = "test-user-1";
        public static readonly Guid BarbellId = Guid.NewGuid();
        public static readonly Guid DumbbellId = Guid.NewGuid();
        public static readonly Guid CableId = Guid.NewGuid();

        public FakeCurrentUserService CurrentUserService { get; }
        public FakeRoutineRepository RoutineRepository { get; }
        public FakeTrainingProfileRepository ProfileRepository { get; }
        public FakeExerciseRepository ExerciseRepository { get; }
        public FakeTrainingSessionRepository SessionRepository { get; }
        public SessionAdaptationProposalGenerator Generator { get; }

        public Ejercicio BenchPress { get; private set; } = null!;
        public Ejercicio DumbbellBenchPress { get; private set; } = null!;
        public Ejercicio CableFlyes { get; private set; } = null!;
        public Ejercicio Squat { get; private set; } = null!;
        public Ejercicio Deadlift { get; private set; } = null!;

        public AdaptiveTestFixture()
        {
            CurrentUserService = new FakeCurrentUserService(UserId);
            RoutineRepository = new FakeRoutineRepository();
            ProfileRepository = new FakeTrainingProfileRepository();
            ExerciseRepository = new FakeExerciseRepository();
            SessionRepository = new FakeTrainingSessionRepository();

            Generator = new SessionAdaptationProposalGenerator(
                RoutineRepository,
                ProfileRepository,
                ExerciseRepository);

            SetupExercisesAndProfile();
        }

        private void SetupExercisesAndProfile()
        {
            BenchPress = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Press de banca plano",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };
            BenchPress.ConfigurarMetadataPlanificacion(
                "bench-press",
                PatronMovimiento.EmpujeHorizontal,
                TipoEjercicio.Compuesto,
                []);
            BenchPress.DefinirEquipamientos([BarbellId]);

            DumbbellBenchPress = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Press con mancuernas plano",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };
            DumbbellBenchPress.ConfigurarMetadataPlanificacion(
                "dumbbell-bench-press",
                PatronMovimiento.EmpujeHorizontal,
                TipoEjercicio.Compuesto,
                []);
            DumbbellBenchPress.DefinirEquipamientos([DumbbellId]);

            CableFlyes = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Cruces de polea",
                GrupoMuscularPrincipal = "Pecho",
                Activo = true
            };
            CableFlyes.ConfigurarMetadataPlanificacion(
                "cable-flyes",
                PatronMovimiento.EmpujeHorizontal,
                TipoEjercicio.Aislamiento,
                []);
            CableFlyes.DefinirEquipamientos([CableId]);

            Squat = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Sentadilla con barra",
                GrupoMuscularPrincipal = "Cuádriceps",
                Activo = true
            };
            Squat.ConfigurarMetadataPlanificacion(
                "squat",
                PatronMovimiento.DominanteRodilla,
                TipoEjercicio.Compuesto,
                []);
            Squat.DefinirEquipamientos([BarbellId]);

            Deadlift = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = "Peso muerto",
                GrupoMuscularPrincipal = "Isquiosurales",
                Activo = true
            };
            Deadlift.ConfigurarMetadataPlanificacion(
                "deadlift",
                PatronMovimiento.BisagraCadera,
                TipoEjercicio.Compuesto,
                []);
            Deadlift.DefinirEquipamientos([BarbellId]);

            ExerciseRepository.Exercises.AddRange([
                BenchPress,
                DumbbellBenchPress,
                CableFlyes,
                Squat,
                Deadlift
            ]);

            var profile = new PerfilEntrenamiento(
                UserId,
                ObjetivoEntrenamiento.GanarMasaMuscular,
                NivelExperiencia.Intermedio,
                25,
                Sexo.Masculino,
                75m,
                4,
                60,
                EntornoEntrenamiento.GimnasioComercial);
            profile.DefinirEquipamientos([BarbellId, DumbbellId]);

            ProfileRepository.Profiles[UserId] = profile;
        }

        public static void SetProperty<T, TProp>(T entity, string propertyName, TProp value) where T : class
        {
            typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(entity, value);
        }

        public DiaRutina SetupRoutineDay()
        {
            var routine = new Rutina(UserId, "Rutina Principal", "Descripción");
            SetProperty(routine, "Id", Guid.NewGuid());
            var day = routine.AgregarDia("Día 1: Empuje", null, 1, DiaSemana.Lunes);
            SetProperty(day, "Id", Guid.NewGuid());
            SetProperty(day, "Rutina", routine);

            // Exercise 1: Compound required bench press (Barbell)
            var e1 = day.AgregarEjercicio(
                BenchPress.Id,
                cantidadSeries: 3,
                repeticionesMinimas: 8,
                repeticionesMaximas: 10,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoSegundos: 120,
                orden: 1,
                notas: null,
                isRequiredForAdaptation: true);
            SetProperty(e1, "Id", Guid.NewGuid());

            // Exercise 2: Optional accessory cable flyes (Cable)
            var e2 = day.AgregarEjercicio(
                CableFlyes.Id,
                cantidadSeries: 3,
                repeticionesMinimas: 12,
                repeticionesMaximas: 15,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoSegundos: 60,
                orden: 2,
                notas: null,
                isRequiredForAdaptation: false);
            SetProperty(e2, "Id", Guid.NewGuid());

            RoutineRepository.Days[day.Id] = day;
            return day;
        }

        public DiaRutina SetupRoutineDayWithMultipleCompounds()
        {
            var routine = new Rutina(UserId, "Rutina Pesada", null);
            SetProperty(routine, "Id", Guid.NewGuid());
            var day = routine.AgregarDia("Día Compuesto", null, 1, DiaSemana.Lunes);
            SetProperty(day, "Id", Guid.NewGuid());
            SetProperty(day, "Rutina", routine);

            var e1 = day.AgregarEjercicio(
                BenchPress.Id,
                cantidadSeries: 3,
                repeticionesMinimas: 5,
                repeticionesMaximas: 5,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoSegundos: 180,
                orden: 1,
                notas: null,
                isRequiredForAdaptation: true);
            SetProperty(e1, "Id", Guid.NewGuid());

            var e2 = day.AgregarEjercicio(
                Squat.Id,
                cantidadSeries: 3,
                repeticionesMinimas: 5,
                repeticionesMaximas: 5,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoSegundos: 180,
                orden: 2,
                notas: null,
                isRequiredForAdaptation: true);
            SetProperty(e2, "Id", Guid.NewGuid());

            var e3 = day.AgregarEjercicio(
                Deadlift.Id,
                cantidadSeries: 3,
                repeticionesMinimas: 5,
                repeticionesMaximas: 5,
                rirObjetivoMinimo: 1,
                rirObjetivoMaximo: 2,
                descansoSegundos: 180,
                orden: 3,
                notas: null,
                isRequiredForAdaptation: true);
            SetProperty(e3, "Id", Guid.NewGuid());

            RoutineRepository.Days[day.Id] = day;
            return day;
        }

        public GenerateSessionAdaptationProposalQueryHandler CreatePreviewHandler()
        {
            return new GenerateSessionAdaptationProposalQueryHandler(
                CurrentUserService,
                Generator);
        }

        public ConfirmSessionAdaptationProposalCommandHandler CreateConfirmHandler()
        {
            return new ConfirmSessionAdaptationProposalCommandHandler(
                CurrentUserService,
                Generator,
                SessionRepository);
        }
    }

    public sealed class FakeCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);

        public FakeCurrentUserService(string? userId)
        {
            UserId = userId;
        }
    }

    public sealed class FakeRoutineRepository : IRoutineRepository
    {
        public Dictionary<Guid, DiaRutina> Days { get; } = [];

        public Task<DiaRutina?> GetDayByIdAndUserIdAsync(
            Guid dayId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (Days.TryGetValue(dayId, out var day) && day.Rutina.UsuarioId == userId)
            {
                return Task.FromResult<DiaRutina?>(day);
            }
            return Task.FromResult<DiaRutina?>(null);
        }

        public Task<IReadOnlyCollection<Rutina>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Rutina?> GetDetailByIdAndUserIdAsync(Guid id, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Rutina?> GetForUpdateByIdAndUserIdAsync(Guid id, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveSessionAsync(Guid routineId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddAsync(Rutina rutina, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RoutineProposalPersistenceResult> ConfirmPlannerProposalAsync(Rutina routine, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Remove(Rutina rutina) => throw new NotImplementedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class FakeTrainingProfileRepository : ITrainingProfileRepository
    {
        public Dictionary<string, PerfilEntrenamiento> Profiles { get; } = [];

        public Task<PerfilEntrenamiento?> GetByUserIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            Profiles.TryGetValue(userId, out var profile);
            return Task.FromResult(profile);
        }

        public Task AddAsync(PerfilEntrenamiento perfil, CancellationToken cancellationToken = default)
        {
            Profiles[perfil.UsuarioId] = perfil;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class FakeExerciseRepository : IExerciseRepository
    {
        public List<Ejercicio> Exercises { get; } = [];

        public Task<IReadOnlyCollection<Ejercicio>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            var active = Exercises.Where(e => e.Activo).ToList();
            return Task.FromResult<IReadOnlyCollection<Ejercicio>>(active);
        }

        public Task<Ejercicio?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Exercises.FirstOrDefault(e => e.Id == id && e.Activo));
        }

        public Task<Ejercicio?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Exercises.FirstOrDefault(e => e.Id == id));
        }

        public Task<IReadOnlyCollection<Guid>> GetValidActiveIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var set = ids.ToHashSet();
            var valid = Exercises.Where(e => e.Activo && set.Contains(e.Id)).Select(e => e.Id).ToList();
            return Task.FromResult<IReadOnlyCollection<Guid>>(valid);
        }
    }

    public sealed class FakeTrainingSessionRepository : ITrainingSessionRepository
    {
        public List<SesionEntrenamiento> AddedSessions { get; } = [];
        public List<SesionAdaptacion> AddedAdaptations { get; } = [];
        public bool HasActiveSessionValue { get; set; } = false;

        public Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HasActiveSessionValue || AddedSessions.Any(s => s.UsuarioId == userId && s.Estado == EstadoSesionEntrenamiento.EnCurso));
        }

        public Task<AdaptiveSessionPersistenceResult> ConfirmAdaptiveSessionAsync(
            SesionEntrenamiento session,
            SesionAdaptacion adaptation,
            CancellationToken cancellationToken = default)
        {
            // Idempotency check
            var existing = AddedAdaptations.FirstOrDefault(a =>
                a.UsuarioId == adaptation.UsuarioId &&
                a.AdaptationProposalId == adaptation.AdaptationProposalId);

            if (existing is not null)
            {
                if (!string.Equals(existing.ProposalHash, adaptation.ProposalHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException("La propuesta ya fue confirmada con otro contenido.");
                }

                return Task.FromResult(new AdaptiveSessionPersistenceResult(
                    existing.SesionEntrenamientoId,
                    existing.AdaptationProposalId,
                    false));
            }

            if (HasActiveSessionValue || AddedSessions.Any(s => s.UsuarioId == session.UsuarioId && s.Estado == EstadoSesionEntrenamiento.EnCurso))
            {
                throw new ConflictException("Ya existe una sesión de entrenamiento en curso.");
            }

            session.AsignarAdaptacion(adaptation);
            AddedSessions.Add(session);
            AddedAdaptations.Add(adaptation);

            return Task.FromResult(new AdaptiveSessionPersistenceResult(
                session.Id,
                adaptation.AdaptationProposalId,
                true));
        }

        public Task AddAsync(SesionEntrenamiento session, CancellationToken cancellationToken = default)
        {
            AddedSessions.Add(session);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SesionEntrenamiento?> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SesionEntrenamiento?> GetByIdAndUserIdAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<TrainingSessionHistorySummaryDto>> GetHistoryByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<DateTime>> GetCompletedSessionStartTimesByUserIdAsync(string userId, DateTime fromUtc, DateTime toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SesionEntrenamiento?> GetByIdAndUserIdAsNoTrackingAsync(Guid sessionId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ExerciseProgressEntryDto>> GetCompletedExerciseProgressTrendByUserIdAsync(Guid exerciseId, string userId, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasCompletedExerciseProgressByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ExerciseProgressHubSourceDto>> GetCompletedExerciseProgressHubByUserIdAsync(string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<ProgressExposure>> GetRecentCompletedExerciseExposuresByUserIdAsync(Guid exerciseId, string userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
