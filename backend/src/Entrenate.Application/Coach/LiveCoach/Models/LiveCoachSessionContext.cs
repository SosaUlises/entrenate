using System;
using System.Collections.Generic;

namespace Entrenate.Application.Coach.LiveCoach.Models
{
    public sealed record LiveCoachExerciseContext(
        Guid ExerciseSessionId,
        Guid ExerciseId,
        string Name,
        int Order,
        string? NextExerciseName = null);

    public sealed record LiveCoachSetContext(
        int SetNumber,
        bool IsTargetSet,
        bool IsAlreadySaved,
        int TargetSetsPlanned);

    public sealed record LiveCoachPrescriptionContext(
        int TargetSets,
        int MinRepetitions,
        int MaxRepetitions,
        int MinRir,
        int MaxRir,
        int RestSeconds,
        string? Notes);

    public sealed record LiveCoachCompletedSetContext(
        string ExerciseName,
        int SetNumber,
        decimal Weight,
        int Repetitions,
        int? Rir,
        DateTime RecordedAtUtc);

    public sealed record LiveCoachRestContext(
        bool IsActive,
        int? TargetSeconds,
        int? RemainingSecondsAtCapture,
        DateTime CapturedAtUtc);

    public sealed record LiveCoachSessionSummaryContext(
        DateTime StartedAtUtc,
        int ExerciseCount,
        int TargetSetCount,
        int CompletedTargetSetCount);

    public sealed record LiveCoachSessionContext(
        Guid SessionId,
        Guid? RoutineDayId,
        DateTime CapturedAtUtc,
        bool IsStaleContext,
        string? DriftNote,
        LiveCoachExerciseContext? CurrentExercise,
        LiveCoachSetContext? CurrentSet,
        LiveCoachPrescriptionContext? CurrentPrescription,
        IReadOnlyList<LiveCoachCompletedSetContext> CompletedSets,
        LiveCoachRestContext Rest,
        LiveCoachSessionSummaryContext Summary);
}

