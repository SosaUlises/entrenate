using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.Adaptation;
using Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
using Entrenate.Application.TrainingSessions.Commands.SkipCurrentExercise;
using Entrenate.Application.TrainingSessions.Commands.ReactivateExercise;
using Entrenate.Application.TrainingSessions.DTOs;
using Entrenate.Application.TrainingSessions.Queries.GetTrainingSessionById;
using Entrenate.Domain.Enums;
using MediatR;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.LiveCoach.Commands
{
    public sealed class ConfirmLiveCoachActionCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenActionIsValid_ReusesTrainingCommandAndReturnsUpdatedSession()
        {
            var action = CreateAction();
            var store = new StubStore(new(PendingLiveCoachActionTakeStatus.Success, action));
            var sender = new RecordingSender(
                action.SessionId,
                CreateSession(
                    action,
                    CreateExercise(action.ExerciseSessionId, 1, 3, 150, completedSets: 1)));
            var handler = new ConfirmLiveCoachActionCommandHandler(
                new LiveCoachPendingActionExecutor(
                    new StubCurrentUser("owner"), store, sender));

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            Assert.Equal(action.SessionId, result.UpdatedSession.Id);
            Assert.Equal("StartRestTimer", result.ClientAction?.Type);
            Assert.Equal(150, result.ClientAction?.DurationSeconds);
            Assert.Equal("Serie registrada. Descanso: 150 segundos.", result.Text);
            var upsert = Assert.IsType<UpsertTrainingSetCommand>(sender.Requests[0]);
            Assert.Equal(action.ExerciseSessionId, upsert.ExerciseSessionId);
            Assert.Equal(action.SetNumber, upsert.SetNumber);
            Assert.Equal(action.WeightKg, upsert.Peso);
            Assert.Equal(action.Repetitions, upsert.Repeticiones);
            Assert.Equal(action.Rir, upsert.Rir);
            Assert.True(upsert.RequireCurrentPendingSet);
            Assert.IsType<GetTrainingSessionByIdQuery>(sender.Requests[1]);
        }

        [Fact]
        public async Task Handle_WhenSkipActionIsValid_UsesAuthoritativeSkipCommand()
        {
            var action = CreateAction() with
            {
                Type = LiveCoachActionTypes.SkipCurrentExercise,
                ExerciseName = "Hip Thrust",
                PendingSetCount = 2
            };
            var updated = CreateSession(
                action,
                CreateExercise(action.ExerciseSessionId, 1, 3, 120, completedSets: 1));
            var sender = new RecordingSender(action.SessionId, updated);
            var handler = CreateHandler(action, updated, sender);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            var skip = Assert.IsType<SkipCurrentExerciseCommand>(sender.Requests.Single());
            Assert.Equal(action.SessionId, skip.SessionId);
            Assert.Equal(action.ExerciseSessionId, skip.ExpectedExerciseSessionId);
            Assert.Equal(action.SetNumber, skip.ExpectedSetNumber);
            Assert.Equal("Ejercicio omitido. Seguimos con el siguiente.", result.Text);
            Assert.Null(result.ClientAction);
        }

        [Fact]
        public async Task Handle_WhenReactivateActionIsValid_ReusesReactivateCommand()
        {
            var action = CreateAction() with
            {
                Type = LiveCoachActionTypes.ReactivateExercise,
                ExerciseName = "Hip Thrust"
            };
            var updated = CreateSession(
                action,
                CreateExercise(action.ExerciseSessionId, 1, 3, 120, completedSets: 1));
            var sender = new RecordingSender(action.SessionId, updated);
            var handler = CreateHandler(action, updated, sender);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            var reactivate = Assert.IsType<ReactivateExerciseCommand>(sender.Requests.Single());
            Assert.Equal(action.SessionId, reactivate.SessionId);
            Assert.Equal(action.ExerciseSessionId, reactivate.ExerciseSessionId);
            Assert.Equal("Hip Thrust reactivado.", result.Text);
            Assert.Null(result.ClientAction);
        }

        [Fact]
        public async Task Handle_WhenTimeAdaptationIsValid_UsesDeterministicApplyCommand()
        {
            var proposal = new LiveTimeConstraintProposal(
                Guid.NewGuid(),
                "owner",
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddSeconds(45),
                20,
                34,
                20,
                "fingerprint",
                [new(LiveTimeConstraintChangeType.OmitExercise, Guid.NewGuid(), "Curl")]);
            var action = CreateAction() with
            {
                SessionId = proposal.SessionId,
                Type = LiveCoachActionTypes.ApplyTimeConstraintAdaptation,
                TimeConstraintProposal = proposal
            };
            var updated = CreateSession(action);
            var sender = new RecordingSender(action.SessionId, updated);
            var handler = CreateHandler(action, updated, sender);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            var command = Assert.IsType<ApplyLiveTimeConstraintAdaptationCommand>(sender.Requests.Single());
            Assert.Equal(proposal, command.Proposal);
            Assert.Equal("Listo. Ajusté la sesión para acercarla a 20 minutos.", result.Text);
        }

        [Fact]
        public async Task Handle_WhenLastSetOfExerciseIsConfirmed_UsesCompletedExerciseRest()
        {
            var action = CreateAction();
            var session = CreateSession(
                action,
                CreateExercise(action.ExerciseSessionId, 1, 1, 150, completedSets: 1),
                CreateExercise(Guid.NewGuid(), 2, 3, 60, completedSets: 0));
            var handler = CreateHandler(action, session);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            Assert.Equal(150, result.ClientAction?.DurationSeconds);
        }

        [Fact]
        public async Task Handle_WhenLastSetOfSessionIsConfirmed_DoesNotStartRest()
        {
            var action = CreateAction();
            var session = CreateSession(
                action,
                CreateExercise(action.ExerciseSessionId, 1, 1, 150, completedSets: 1));
            var handler = CreateHandler(action, session);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Serie registrada. Completaste todas las series.", result.Text);
        }

        [Fact]
        public async Task Handle_WhenRestIsMissing_DoesNotStartTimer()
        {
            var action = CreateAction();
            var session = CreateSession(
                action,
                CreateExercise(action.ExerciseSessionId, 1, 3, 0, completedSets: 1));
            var handler = CreateHandler(action, session);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: false),
                CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Serie registrada.", result.Text);
        }

        [Fact]
        public async Task Handle_WhenTimerIsAlreadyActive_RegistersWithoutRestartingIt()
        {
            var action = CreateAction();
            var session = CreateSession(
                action,
                CreateExercise(action.ExerciseSessionId, 1, 3, 150, completedSets: 1));
            var sender = new RecordingSender(action.SessionId, session);
            var handler = CreateHandler(action, session, sender);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId, RestTimerIsActive: true),
                CancellationToken.None);

            Assert.Null(result.ClientAction);
            Assert.Equal("Serie registrada.", result.Text);
            Assert.IsType<UpsertTrainingSetCommand>(sender.Requests[0]);
        }

        [Theory]
        [InlineData(PendingLiveCoachActionTakeStatus.Expired)]
        [InlineData(PendingLiveCoachActionTakeStatus.NotFound)]
        public async Task Handle_WhenActionCannotBeConsumed_ReturnsSafeConflict(
            PendingLiveCoachActionTakeStatus status)
        {
            var handler = new ConfirmLiveCoachActionCommandHandler(
                new LiveCoachPendingActionExecutor(
                    new StubCurrentUser("owner"),
                    new StubStore(new(status)),
                    new RecordingSender(Guid.NewGuid())));

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new ConfirmLiveCoachActionCommand(Guid.NewGuid(), RestTimerIsActive: false),
                CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenActionBelongsToAnotherUser_DoesNotExposeIt()
        {
            var handler = new ConfirmLiveCoachActionCommandHandler(
                new LiveCoachPendingActionExecutor(
                    new StubCurrentUser("other"),
                    new StubStore(new(PendingLiveCoachActionTakeStatus.NotOwned)),
                    new RecordingSender(Guid.NewGuid())));

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new ConfirmLiveCoachActionCommand(Guid.NewGuid(), RestTimerIsActive: false),
                CancellationToken.None));
        }

        private static PendingLiveCoachAction CreateAction() => new(
            Guid.NewGuid(), "owner", Guid.NewGuid(), Guid.NewGuid(), 1,
            100m, 8, 1, DateTime.UtcNow.AddSeconds(45));

        private static ConfirmLiveCoachActionCommandHandler CreateHandler(
            PendingLiveCoachAction action,
            TrainingSessionDto session,
            RecordingSender? sender = null)
            => new(new LiveCoachPendingActionExecutor(
                new StubCurrentUser("owner"),
                new StubStore(new(PendingLiveCoachActionTakeStatus.Success, action)),
                sender ?? new RecordingSender(action.SessionId, session)));

        private static TrainingSessionDto CreateSession(
            PendingLiveCoachAction action,
            params TrainingSessionExerciseDto[] exercises)
            => new(
                action.SessionId,
                null,
                DateTime.UtcNow.Date,
                DateTime.UtcNow,
                null,
                EstadoSesionEntrenamiento.EnCurso,
                exercises);

        private static TrainingSessionExerciseDto CreateExercise(
            Guid id,
            int order,
            int targetSets,
            int restSeconds,
            int completedSets)
            => new(
                id,
                Guid.NewGuid(),
                $"Ejercicio {order}",
                order,
                targetSets,
                8,
                10,
                1,
                2,
                restSeconds,
                null,
                Enumerable.Range(1, completedSets)
                    .Select(number => new TrainingSetDto(
                        Guid.NewGuid(), number, 100m, 8, 1, true, DateTime.UtcNow))
                    .ToArray());

        private sealed class StubCurrentUser(string userId) : ICurrentUserService
        {
            public string? UserId { get; } = userId;
        }

        private sealed class StubStore(PendingLiveCoachActionTakeResult result)
            : ILiveCoachPendingActionStore
        {
            public void Set(PendingLiveCoachAction action) { }
            public PendingLiveCoachActionLookupResult FindActive(string userId, Guid sessionId, DateTime nowUtc)
                => new(PendingLiveCoachActionLookupStatus.NotFound);
            public PendingLiveCoachActionTakeResult Take(Guid actionId, string userId, DateTime nowUtc) => result;
            public PendingLiveCoachActionTakeResult Cancel(Guid actionId, string userId, DateTime nowUtc) => result;
            public PendingLiveCoachActionUpdateResult Update(
                Guid actionId,
                string userId,
                PendingLiveCoachSetPatch patch,
                DateTime nowUtc)
                => new(PendingLiveCoachActionUpdateStatus.NotFound);
        }

        private sealed class RecordingSender(
            Guid sessionId,
            TrainingSessionDto? updatedSession = null) : ISender
        {
            public List<object> Requests { get; } = [];

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                object response = request switch
                {
                    UpsertTrainingSetCommand command => new TrainingSetDto(
                        Guid.NewGuid(), command.SetNumber, command.Peso, command.Repeticiones,
                        command.Rir, true, DateTime.UtcNow),
                    SkipCurrentExerciseCommand => updatedSession ?? throw new InvalidOperationException(),
                    ReactivateExerciseCommand => updatedSession ?? throw new InvalidOperationException(),
                    ApplyLiveTimeConstraintAdaptationCommand => updatedSession ?? throw new InvalidOperationException(),
                    GetTrainingSessionByIdQuery => updatedSession ?? new TrainingSessionDto(
                        sessionId, null, DateTime.UtcNow.Date, DateTime.UtcNow,
                        null, EstadoSesionEntrenamiento.EnCurso, []),
                    _ => throw new NotSupportedException(request.GetType().Name)
                };
                return Task.FromResult((TResponse)response);
            }

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                Requests.Add(request!);
                return Task.CompletedTask;
            }

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }
    }
}
