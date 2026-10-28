using Entrenate.Application.Coach.RoutinePlanner.Commands.ConfirmRoutineProposal;
using Entrenate.Application.Coach.RoutinePlanner.Confirmation;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.Confirmation
{
    public sealed class ConfirmRoutineProposalCommandHandlerTests
    {
        private const string UserId = "routine-planner-user";

        [Fact]
        public async Task Handle_ConfirmsProposalAndReplacesActiveRoutine()
        {
            var context = CreateContext(CreateCompleteCatalog());
            var previousRoutine = new Rutina(UserId, "Anterior", null);
            context.RoutineRepository.AddExisting(previousRoutine);
            var proposal = await context.Generator.GenerateForUserAsync(UserId);

            var result = await context.Handler.Handle(
                new ConfirmRoutineProposalCommand(
                    proposal.ProposalId,
                    proposal.CanonicalHash),
                CancellationToken.None);

            Assert.True(result.CreatedNow);
            Assert.True(result.IsActive);
            Assert.Equal(proposal.ProposalId, result.ProposalId);
            Assert.False(previousRoutine.Activa);

            var persisted = context.RoutineRepository.LastRoutine;
            Assert.True(persisted.Activa);
            Assert.Equal(proposal.ProposalId, persisted.PlannerProposalId);
            Assert.Equal(proposal.CanonicalHash, persisted.PlannerProposalHash);
            Assert.Single(persisted.Dias);
            Assert.Equal(DiaSemana.Lunes, persisted.Dias.Single().DiaSemana);
            Assert.Equal(5, persisted.Dias.Single().Ejercicios.Count);
        }

        [Fact]
        public async Task Handle_PersistsAdaptationIntentFromPlannerSlots()
        {
            var context = CreateContext(
                CreateCompleteCatalog(),
                daysPerWeek: 4);
            var proposal = await context.Generator.GenerateForUserAsync(UserId);

            await context.Handler.Handle(
                new ConfirmRoutineProposalCommand(
                    proposal.ProposalId,
                    proposal.CanonicalHash),
                CancellationToken.None);

            var expectedExercises = proposal.Days
                .SelectMany(day => day.Exercises.Select(exercise => new
                {
                    DayOrder = day.Order,
                    Exercise = exercise
                }))
                .ToArray();
            Assert.Contains(
                expectedExercises,
                item => item.Exercise.IsRequired);
            Assert.Contains(
                expectedExercises,
                item => !item.Exercise.IsRequired);

            foreach (var expected in expectedExercises)
            {
                var persisted = context.RoutineRepository.LastRoutine.Dias
                    .Single(day => day.Orden == expected.DayOrder)
                    .Ejercicios
                    .Single(exercise =>
                        exercise.Orden == expected.Exercise.Order);

                Assert.Equal(
                    expected.Exercise.IsRequired,
                    persisted.IsRequiredForAdaptation);
            }
        }

        [Fact]
        public async Task Handle_SameProposalTwiceReturnsSameRoutine()
        {
            var context = CreateContext(CreateCompleteCatalog());
            var proposal = await context.Generator.GenerateForUserAsync(UserId);
            var command = new ConfirmRoutineProposalCommand(
                proposal.ProposalId,
                proposal.CanonicalHash);

            var first = await context.Handler.Handle(
                command,
                CancellationToken.None);
            var second = await context.Handler.Handle(
                command,
                CancellationToken.None);

            Assert.True(first.CreatedNow);
            Assert.False(second.CreatedNow);
            Assert.Equal(first.RoutineId, second.RoutineId);
            Assert.Single(context.RoutineRepository.PlannerRoutines);
        }

        [Fact]
        public async Task Handle_StaleIdentityOrHashDoesNotPersist()
        {
            var context = CreateContext(CreateCompleteCatalog());
            var proposal = await context.Generator.GenerateForUserAsync(UserId);

            await Assert.ThrowsAsync<ConflictException>(() =>
                context.Handler.Handle(
                    new ConfirmRoutineProposalCommand(
                        Guid.NewGuid(),
                        proposal.CanonicalHash),
                    CancellationToken.None));
            await Assert.ThrowsAsync<ConflictException>(() =>
                context.Handler.Handle(
                    new ConfirmRoutineProposalCommand(
                        proposal.ProposalId,
                        new string('0', 64)),
                    CancellationToken.None));

            Assert.Equal(0, context.RoutineRepository.ConfirmationCount);
            Assert.Empty(context.RoutineRepository.PlannerRoutines);
        }

        [Fact]
        public async Task Handle_NotFeasibleProposalDoesNotPersist()
        {
            var context = CreateContext([]);
            var proposal = await context.Generator.GenerateForUserAsync(UserId);

            await Assert.ThrowsAsync<ConflictException>(() =>
                context.Handler.Handle(
                    new ConfirmRoutineProposalCommand(
                        proposal.ProposalId,
                        proposal.CanonicalHash),
                    CancellationToken.None));

            Assert.Equal(
                RoutineProposalFeasibility.NotFeasible,
                proposal.Feasibility);
            Assert.Equal(0, context.RoutineRepository.ConfirmationCount);
        }

        private static TestContext CreateContext(
            IReadOnlyCollection<Ejercicio> exercises,
            int daysPerWeek = 1)
        {
            var profile = new PerfilEntrenamiento(
                UserId,
                ObjetivoEntrenamiento.GanarFuerza,
                NivelExperiencia.Principiante,
                30,
                null,
                null,
                daysPerWeek,
                120,
                EntornoEntrenamiento.Calistenia);
            profile.DefinirDiasPreferidos(
                new[]
                {
                    DiaSemana.Lunes,
                    DiaSemana.Martes,
                    DiaSemana.Jueves,
                    DiaSemana.Viernes
                }
                .Take(daysPerWeek));

            var generator = new RoutinePlannerProposalGenerator(
                new TrainingProfileRepositoryFake(profile),
                new ExerciseRepositoryFake(exercises),
                new EquipmentRepositoryFake());
            var routineRepository = new RoutineRepositoryFake();
            var handler = new ConfirmRoutineProposalCommandHandler(
                new CurrentUserServiceFake(UserId),
                generator,
                routineRepository);

            return new TestContext(
                generator,
                handler,
                routineRepository);
        }

        private static IReadOnlyCollection<Ejercicio> CreateCompleteCatalog()
        {
            return
            [
                Exercise(
                    "sentadilla-corporal",
                    "Cuádriceps",
                    PatronMovimiento.DominanteRodilla,
                    TipoEjercicio.Compuesto),
                Exercise(
                    "bisagra-corporal",
                    "Isquiotibiales",
                    PatronMovimiento.BisagraCadera,
                    TipoEjercicio.Compuesto),
                Exercise(
                    "flexiones",
                    "Pecho",
                    PatronMovimiento.EmpujeHorizontal,
                    TipoEjercicio.Compuesto),
                Exercise(
                    "remo-corporal",
                    "Espalda",
                    PatronMovimiento.TraccionHorizontal,
                    TipoEjercicio.Compuesto),
                Exercise(
                    "press-vertical",
                    "Hombros",
                    PatronMovimiento.EmpujeVertical,
                    TipoEjercicio.Compuesto),
                Exercise(
                    "dominadas",
                    "Espalda",
                    PatronMovimiento.TraccionVertical,
                    TipoEjercicio.Compuesto),
                Exercise(
                    "elevaciones-laterales",
                    "Hombros",
                    PatronMovimiento.AbduccionHombro,
                    TipoEjercicio.Aislamiento),
                Exercise(
                    "curl-biceps",
                    "Bíceps",
                    PatronMovimiento.FlexionCodo,
                    TipoEjercicio.Aislamiento),
                Exercise(
                    "extension-triceps",
                    "Tríceps",
                    PatronMovimiento.ExtensionCodo,
                    TipoEjercicio.Aislamiento),
                Exercise(
                    "extension-cuadriceps",
                    "Cuádriceps",
                    PatronMovimiento.ExtensionRodilla,
                    TipoEjercicio.Aislamiento),
                Exercise(
                    "curl-femoral",
                    "Isquiotibiales",
                    PatronMovimiento.FlexionRodilla,
                    TipoEjercicio.Aislamiento),
                Exercise(
                    "elevacion-gemelos",
                    "Gemelos",
                    PatronMovimiento.FlexionPlantar,
                    TipoEjercicio.Aislamiento),
                Exercise(
                    "plancha",
                    "Core",
                    PatronMovimiento.CoreAntiExtension,
                    TipoEjercicio.Aislamiento)
            ];
        }

        private static Ejercicio Exercise(
            string code,
            string primaryMuscleGroup,
            PatronMovimiento movementPattern,
            TipoEjercicio exerciseType)
        {
            var exercise = new Ejercicio
            {
                Id = Guid.NewGuid(),
                Nombre = code,
                GrupoMuscularPrincipal = primaryMuscleGroup,
                Activo = true
            };
            exercise.ConfigurarMetadataPlanificacion(
                code,
                movementPattern,
                exerciseType,
                []);

            return exercise;
        }

        private sealed record TestContext(
            RoutinePlannerProposalGenerator Generator,
            ConfirmRoutineProposalCommandHandler Handler,
            RoutineRepositoryFake RoutineRepository);

        private sealed class CurrentUserServiceFake(string userId)
            : ICurrentUserService
        {
            public string? UserId { get; } = userId;
        }

        private sealed class TrainingProfileRepositoryFake(
            PerfilEntrenamiento profile)
            : ITrainingProfileRepository
        {
            public Task<PerfilEntrenamiento?> GetByUserIdAsync(
                string userId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<PerfilEntrenamiento?>(
                    profile.UsuarioId == userId ? profile : null);
            }

            public Task AddAsync(
                PerfilEntrenamiento perfil,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task SaveChangesAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class ExerciseRepositoryFake(
            IReadOnlyCollection<Ejercicio> exercises)
            : IExerciseRepository
        {
            public Task<IReadOnlyCollection<Ejercicio>> GetActiveAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(exercises);
            }

            public Task<Ejercicio?> GetActiveByIdAsync(
                Guid id,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(exercises.SingleOrDefault(
                    item => item.Id == id && item.Activo));
            }

            public Task<Ejercicio?> GetByIdAsync(
                Guid id,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(exercises.SingleOrDefault(
                    item => item.Id == id));
            }

            public Task<IReadOnlyCollection<Guid>> GetValidActiveIdsAsync(
                IEnumerable<Guid> ids,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyCollection<Guid> result = exercises
                    .Where(item => item.Activo && ids.Contains(item.Id))
                    .Select(item => item.Id)
                    .ToArray();

                return Task.FromResult(result);
            }
        }

        private sealed class EquipmentRepositoryFake : IEquipmentRepository
        {
            public Task<IReadOnlyCollection<Equipamiento>> GetActiveAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<Equipamiento>>([]);
            }

            public Task<IReadOnlyCollection<Equipamiento>> GetByIdsAsync(
                IEnumerable<Guid> ids,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<Equipamiento>>([]);
            }

            public Task<IReadOnlyCollection<Guid>> GetValidActiveIdsAsync(
                IEnumerable<Guid> ids,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<Guid>>([]);
            }
        }

        private sealed class RoutineRepositoryFake : IRoutineRepository
        {
            private readonly List<(Guid Id, Rutina Routine)> _routines = [];

            public int ConfirmationCount { get; private set; }

            public IReadOnlyCollection<Rutina> PlannerRoutines => _routines
                .Where(item => item.Routine.PlannerProposalId.HasValue)
                .Select(item => item.Routine)
                .ToArray();

            public Rutina LastRoutine => _routines[^1].Routine;

            public void AddExisting(Rutina routine)
            {
                _routines.Add((Guid.NewGuid(), routine));
            }

            public Task<RoutineProposalPersistenceResult>
                ConfirmPlannerProposalAsync(
                    Rutina routine,
                    CancellationToken cancellationToken = default)
            {
                ConfirmationCount++;
                var existing = _routines.SingleOrDefault(item =>
                    item.Routine.UsuarioId == routine.UsuarioId &&
                    item.Routine.PlannerProposalId ==
                        routine.PlannerProposalId);

                if (existing.Routine is not null)
                {
                    if (!string.Equals(
                        existing.Routine.PlannerProposalHash,
                        routine.PlannerProposalHash,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ConflictException(
                            "La propuesta ya existe con otro contenido.");
                    }

                    return Task.FromResult(
                        new RoutineProposalPersistenceResult(
                            existing.Id,
                            existing.Routine.Activa,
                            false));
                }

                foreach (var item in _routines.Where(item =>
                    item.Routine.UsuarioId == routine.UsuarioId &&
                    item.Routine.Activa))
                {
                    item.Routine.Desactivar();
                }

                var id = Guid.NewGuid();
                _routines.Add((id, routine));

                return Task.FromResult(
                    new RoutineProposalPersistenceResult(
                        id,
                        routine.Activa,
                        true));
            }

            public Task<IReadOnlyCollection<Rutina>> GetByUserIdAsync(
                string userId,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyCollection<Rutina> result = _routines
                    .Where(item => item.Routine.UsuarioId == userId)
                    .Select(item => item.Routine)
                    .ToArray();

                return Task.FromResult(result);
            }

            public Task<Rutina?> GetDetailByIdAndUserIdAsync(
                Guid id,
                string userId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<Rutina?>(_routines
                    .SingleOrDefault(item =>
                        item.Id == id &&
                        item.Routine.UsuarioId == userId)
                    .Routine);
            }

            public Task<Rutina?> GetForUpdateByIdAndUserIdAsync(
                Guid id,
                string userId,
                CancellationToken cancellationToken = default)
            {
                return GetDetailByIdAndUserIdAsync(
                    id,
                    userId,
                    cancellationToken);
            }

            public Task<DiaRutina?> GetDayByIdAndUserIdAsync(
                Guid dayId,
                string userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> HasActiveSessionAsync(
                Guid routineId,
                string userId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task AddAsync(
                Rutina rutina,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public void Remove(Rutina rutina)
            {
                throw new NotSupportedException();
            }

            public Task SaveChangesAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
