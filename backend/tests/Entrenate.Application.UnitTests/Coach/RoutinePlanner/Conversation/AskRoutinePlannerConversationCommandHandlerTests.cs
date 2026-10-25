using System.Text.Json;
using Entrenate.Application.Coach.RoutinePlanner.Conversation.Commands.AskRoutinePlannerConversation;
using Entrenate.Application.Coach.RoutinePlanner.Proposal;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.Common.Models;
using Entrenate.Domain.Entidades;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.Conversation
{
    public sealed class AskRoutinePlannerConversationCommandHandlerTests
    {
        private const string UserId = "routine-conversation-user";

        [Fact]
        public async Task Handle_ValidProposalBuildsConversation()
        {
            var context = CreateContext(CreateCompleteCatalog());
            var proposal = await context.Generator.GenerateForUserAsync(UserId);

            var result = await context.Handler.Handle(
                new AskRoutinePlannerConversationCommand(
                    proposal.ProposalId,
                    proposal.CanonicalHash,
                    "¿Por qué elegiste esta distribución?"),
                CancellationToken.None);

            Assert.Equal("Respuesta del Coach.", result.Answer);
            Assert.Equal(1, context.Provider.CallCount);
            Assert.NotNull(context.Provider.LastPrompt);
            using var promptInput = JsonDocument.Parse(
                context.Provider.LastPrompt.Input);
            Assert.Equal(
                "¿Por qué elegiste esta distribución?",
                promptInput.RootElement
                    .GetProperty("question")
                    .GetString());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_StaleIdentityOrHashDoesNotCallProvider(
            bool changeProposalId)
        {
            var context = CreateContext(CreateCompleteCatalog());
            var proposal = await context.Generator.GenerateForUserAsync(UserId);
            var command = new AskRoutinePlannerConversationCommand(
                changeProposalId ? Guid.NewGuid() : proposal.ProposalId,
                changeProposalId
                    ? proposal.CanonicalHash
                    : new string('0', 64),
                "¿Por qué elegiste esta distribución?");

            await Assert.ThrowsAsync<ConflictException>(() =>
                context.Handler.Handle(command, CancellationToken.None));

            Assert.Equal(0, context.Provider.CallCount);
        }

        [Fact]
        public async Task Handle_NotFeasibleProposalAllowsConversation()
        {
            var context = CreateContext([]);
            var proposal = await context.Generator.GenerateForUserAsync(UserId);

            var result = await context.Handler.Handle(
                new AskRoutinePlannerConversationCommand(
                    proposal.ProposalId,
                    proposal.CanonicalHash,
                    "¿Por qué no se pudo generar?"),
                CancellationToken.None);

            Assert.Equal(
                RoutineProposalFeasibility.NotFeasible,
                proposal.Feasibility);
            Assert.Equal("Respuesta del Coach.", result.Answer);
            Assert.Equal(1, context.Provider.CallCount);
        }

        [Fact]
        public async Task Handle_ProviderFailureIsExposedAsExternalServiceError()
        {
            var context = CreateContext(CreateCompleteCatalog());
            var proposal = await context.Generator.GenerateForUserAsync(UserId);
            context.Provider.ThrowOnCall = true;

            await Assert.ThrowsAsync<ExternalServiceException>(() =>
                context.Handler.Handle(
                    new AskRoutinePlannerConversationCommand(
                        proposal.ProposalId,
                        proposal.CanonicalHash,
                        "¿Cuánto dura cada sesión?"),
                    CancellationToken.None));
        }

        [Fact]
        public void Validator_RejectsWhitespaceAndQuestionsOverLimit()
        {
            var validator =
                new AskRoutinePlannerConversationCommandValidator();

            var whitespace = validator.Validate(
                new AskRoutinePlannerConversationCommand(
                    Guid.NewGuid(),
                    new string('a', 64),
                    "   "));
            var overLimit = validator.Validate(
                new AskRoutinePlannerConversationCommand(
                    Guid.NewGuid(),
                    new string('a', 64),
                    new string('a', 501)));

            Assert.False(whitespace.IsValid);
            Assert.False(overLimit.IsValid);
        }

        private static TestContext CreateContext(
            IReadOnlyCollection<Ejercicio> exercises)
        {
            var profile = new PerfilEntrenamiento(
                UserId,
                ObjetivoEntrenamiento.GanarFuerza,
                NivelExperiencia.Principiante,
                30,
                null,
                null,
                1,
                120,
                EntornoEntrenamiento.Calistenia);
            profile.DefinirDiasPreferidos([DiaSemana.Lunes]);

            var generator = new RoutinePlannerProposalGenerator(
                new TrainingProfileRepositoryFake(profile),
                new ExerciseRepositoryFake(exercises),
                new EquipmentRepositoryFake());
            var provider = new ConversationProviderFake();
            var handler =
                new AskRoutinePlannerConversationCommandHandler(
                    new CurrentUserServiceFake(UserId),
                    generator,
                    provider);

            return new TestContext(generator, handler, provider);
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
            AskRoutinePlannerConversationCommandHandler Handler,
            ConversationProviderFake Provider);

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
                    exercise => exercise.Id == id && exercise.Activo));
            }

            public Task<Ejercicio?> GetByIdAsync(
                Guid id,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(exercises.SingleOrDefault(
                    exercise => exercise.Id == id));
            }

            public Task<IReadOnlyCollection<Guid>> GetValidActiveIdsAsync(
                IEnumerable<Guid> ids,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyCollection<Guid> result = exercises
                    .Where(exercise =>
                        exercise.Activo && ids.Contains(exercise.Id))
                    .Select(exercise => exercise.Id)
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

        private sealed class ConversationProviderFake
            : ICoachConversationProvider
        {
            public int CallCount { get; private set; }

            public bool ThrowOnCall { get; set; }

            public CoachConversationPrompt? LastPrompt { get; private set; }

            public Task<CoachConversationResponse> GenerateResponseAsync(
                CoachConversationPrompt prompt,
                CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastPrompt = prompt;

                if (ThrowOnCall)
                {
                    throw new InvalidOperationException(
                        "Provider no disponible.");
                }

                return Task.FromResult(
                    new CoachConversationResponse("Respuesta del Coach."));
            }
        }
    }
}
