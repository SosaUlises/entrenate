using Entrenate.Application.Coach.LiveCoach.Actions;
using Entrenate.Application.Coach.LiveCoach.Commands.ConfirmLiveCoachAction;
using Entrenate.Application.Common.Exceptions;
using Entrenate.Application.Common.Interfaces;
using Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet;
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
            var sender = new RecordingSender(action.SessionId);
            var handler = new ConfirmLiveCoachActionCommandHandler(
                new StubCurrentUser("owner"), store, sender);

            var result = await handler.Handle(
                new ConfirmLiveCoachActionCommand(action.ActionId),
                CancellationToken.None);

            Assert.Equal(action.SessionId, result.Id);
            var upsert = Assert.IsType<UpsertTrainingSetCommand>(sender.Requests[0]);
            Assert.Equal(action.ExerciseSessionId, upsert.ExerciseSessionId);
            Assert.Equal(action.SetNumber, upsert.SetNumber);
            Assert.Equal(action.WeightKg, upsert.Peso);
            Assert.Equal(action.Repetitions, upsert.Repeticiones);
            Assert.Equal(action.Rir, upsert.Rir);
            Assert.True(upsert.RequireCurrentPendingSet);
            Assert.IsType<GetTrainingSessionByIdQuery>(sender.Requests[1]);
        }

        [Theory]
        [InlineData(PendingLiveCoachActionTakeStatus.Expired)]
        [InlineData(PendingLiveCoachActionTakeStatus.NotFound)]
        public async Task Handle_WhenActionCannotBeConsumed_ReturnsSafeConflict(
            PendingLiveCoachActionTakeStatus status)
        {
            var handler = new ConfirmLiveCoachActionCommandHandler(
                new StubCurrentUser("owner"),
                new StubStore(new(status)),
                new RecordingSender(Guid.NewGuid()));

            await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
                new ConfirmLiveCoachActionCommand(Guid.NewGuid()),
                CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WhenActionBelongsToAnotherUser_DoesNotExposeIt()
        {
            var handler = new ConfirmLiveCoachActionCommandHandler(
                new StubCurrentUser("other"),
                new StubStore(new(PendingLiveCoachActionTakeStatus.NotOwned)),
                new RecordingSender(Guid.NewGuid()));

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
                new ConfirmLiveCoachActionCommand(Guid.NewGuid()),
                CancellationToken.None));
        }

        private static PendingLiveCoachAction CreateAction() => new(
            Guid.NewGuid(), "owner", Guid.NewGuid(), Guid.NewGuid(), 1,
            100m, 8, 1, DateTime.UtcNow.AddSeconds(45));

        private sealed class StubCurrentUser(string userId) : ICurrentUserService
        {
            public string? UserId { get; } = userId;
        }

        private sealed class StubStore(PendingLiveCoachActionTakeResult result)
            : ILiveCoachPendingActionStore
        {
            public void Set(PendingLiveCoachAction action) { }
            public PendingLiveCoachActionTakeResult Take(Guid actionId, string userId, DateTime nowUtc) => result;
            public PendingLiveCoachActionTakeResult Cancel(Guid actionId, string userId, DateTime nowUtc) => result;
        }

        private sealed class RecordingSender(Guid sessionId) : ISender
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
                    GetTrainingSessionByIdQuery => new TrainingSessionDto(
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
