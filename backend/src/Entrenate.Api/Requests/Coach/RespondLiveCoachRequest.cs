using System;

namespace Entrenate.Api.Requests.Coach
{
    public sealed class RespondLiveCoachRequest
    {
        public Guid SessionId { get; set; }

        public string ClientTurnId { get; set; } = string.Empty;

        public string InputKind { get; set; } = "AudioTranscript";

        public string Text { get; set; } = string.Empty;

        public LiveCoachClientSnapshotRequest? ClientSnapshot { get; set; }
    }

    public sealed class LiveCoachClientSnapshotRequest
    {
        public Guid? ExerciseId { get; set; }

        public int? SetIndex { get; set; }

        public LiveCoachRestTimerSnapshotRequest? RestTimer { get; set; }
    }

    public sealed class LiveCoachRestTimerSnapshotRequest
    {
        public bool IsActive { get; set; }

        public int RemainingSeconds { get; set; }

        public DateTime CapturedAtUtc { get; set; }
    }
}

