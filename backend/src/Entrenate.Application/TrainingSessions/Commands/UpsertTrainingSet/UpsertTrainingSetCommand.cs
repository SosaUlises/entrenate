using Entrenate.Application.TrainingSessions.DTOs;
using MediatR;

namespace Entrenate.Application.TrainingSessions.Commands.UpsertTrainingSet
{
    public record UpsertTrainingSetCommand(
        Guid SessionId,
        Guid ExerciseSessionId,
        int SetNumber,
        decimal Peso,
        int Repeticiones,
        int? Rir)
        : IRequest<TrainingSetDto>;
}
