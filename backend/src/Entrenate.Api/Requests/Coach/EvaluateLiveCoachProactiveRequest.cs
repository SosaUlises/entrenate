namespace Entrenate.Api.Requests.Coach
{
    public sealed class EvaluateLiveCoachProactiveRequest
    {
        public Guid SessionId { get; set; }

        public LiveCoachProactiveSnapshotRequest? Snapshot { get; set; }
    }

    public sealed class LiveCoachProactiveSnapshotRequest
    {
        public Guid? RestExerciseSessionId { get; set; }

        public DateTime? RestStartedAtUtc { get; set; }
    }
}
