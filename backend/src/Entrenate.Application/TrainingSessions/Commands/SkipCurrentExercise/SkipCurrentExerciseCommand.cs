using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.SkipCurrentExercise
{
    public sealed record SkipCurrentExerciseCommand(
        Guid SessionId,
        Guid? ExpectedExerciseSessionId = null,
        int? ExpectedSetNumber = null) : IRequest<TrainingSessionDto>;
}
