using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.ReactivateExercise
{
    public sealed record ReactivateExerciseCommand(
        Guid SessionId,
        Guid ExerciseSessionId) : IRequest<TrainingSessionDto>;
}
