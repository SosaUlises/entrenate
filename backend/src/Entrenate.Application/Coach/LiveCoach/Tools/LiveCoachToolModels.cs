using System;
using System.Collections.Generic;

namespace Entrenate.Application.Coach.LiveCoach.Tools
{
    public enum LiveCoachToolStatus
    {
        Available,
        NotAvailable,
        StaleContext,
        Unauthorized,
        Failed
    }

    public sealed record LiveCoachToolResult<T>(
        LiveCoachToolStatus Status,
        T? Data,
        string? SafeMessage = null);

    public sealed record PreviousPerformanceItem(
        DateTime SessionDate,
        decimal Weight,
        int Repetitions,
        int? Rir);

    public sealed record PreviousPerformanceData(
        string ExerciseName,
        IReadOnlyList<PreviousPerformanceItem> Exposures);
}

