using Entrenate.Domain.Enums;

namespace Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure
{
    public static class WeeklyRoutineStructurePlanner
    {
        public const string PolicyVersion = "2.1";

        private static readonly IReadOnlyDictionary<
            int,
            IReadOnlyList<SessionDefinition>> SessionDefinitions =
                new Dictionary<int, IReadOnlyList<SessionDefinition>>
                {
                    [1] =
                    [
                        new(RoutineSessionFocus.FullBody,
                            RoutineSessionVariant.A)
                    ],
                    [2] =
                    [
                        new(RoutineSessionFocus.FullBody,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.FullBody,
                            RoutineSessionVariant.B)
                    ],
                    [3] =
                    [
                        new(RoutineSessionFocus.FullBody,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.FullBody,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.FullBody,
                            RoutineSessionVariant.C)
                    ],
                    [4] =
                    [
                        new(RoutineSessionFocus.Upper,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Lower,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Upper,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.Lower,
                            RoutineSessionVariant.B)
                    ],
                    [5] =
                    [
                        new(RoutineSessionFocus.Upper,
                            RoutineSessionVariant.None),
                        new(RoutineSessionFocus.Lower,
                            RoutineSessionVariant.None),
                        new(RoutineSessionFocus.Push,
                            RoutineSessionVariant.None),
                        new(RoutineSessionFocus.Pull,
                            RoutineSessionVariant.None),
                        new(RoutineSessionFocus.Legs,
                            RoutineSessionVariant.None)
                    ],
                    [6] =
                    [
                        new(RoutineSessionFocus.Push,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Pull,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Legs,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Push,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.Pull,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.Legs,
                            RoutineSessionVariant.B)
                    ],
                    [7] =
                    [
                        new(RoutineSessionFocus.Push,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Pull,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Legs,
                            RoutineSessionVariant.A),
                        new(RoutineSessionFocus.Push,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.Pull,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.Legs,
                            RoutineSessionVariant.B),
                        new(RoutineSessionFocus.CoreCalves,
                            RoutineSessionVariant.None)
                    ]
                };

        public static WeeklyRoutineStructure Build(
            int daysPerWeek,
            IReadOnlyCollection<DiaSemana> preferredDays)
        {
            ArgumentNullException.ThrowIfNull(preferredDays);

            if (daysPerWeek is < 1 or > 7)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(daysPerWeek),
                    "Los días semanales deben estar entre 1 y 7.");
            }

            if (preferredDays.Any(day => !Enum.IsDefined(day)))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(preferredDays),
                    "Todos los días preferidos deben ser válidos.");
            }

            var preferredDayValues = preferredDays
                .Select(day => (int)day)
                .Distinct()
                .Order()
                .ToArray();

            if (preferredDayValues.Length > daysPerWeek)
            {
                throw new ArgumentException(
                    "Los días preferidos no pueden superar los días " +
                        "semanales solicitados.",
                    nameof(preferredDays));
            }

            var scheduledDays = SelectScheduledDays(
                daysPerWeek,
                preferredDayValues);
            var preferredDaySet = preferredDayValues.ToHashSet();
            var definitions = SessionDefinitions[daysPerWeek];

            var days = scheduledDays
                .Select((day, index) => new WeeklyRoutineDay(
                    index + 1,
                    (DiaSemana)day,
                    preferredDaySet.Contains(day)
                        ? ScheduledDayOrigin.Preferred
                        : ScheduledDayOrigin.Derived,
                    definitions[index].Focus,
                    definitions[index].Variant))
                .ToArray();

            return new WeeklyRoutineStructure(daysPerWeek, days);
        }

        private static int[] SelectScheduledDays(
            int daysPerWeek,
            IReadOnlyCollection<int> preferredDays)
        {
            var preferredMask = preferredDays.Aggregate(
                0,
                (mask, day) => mask | (1 << (day - 1)));
            int[]? bestDays = null;
            int[]? bestSeparation = null;

            for (var mask = 1; mask < 1 << 7; mask++)
            {
                if ((mask & preferredMask) != preferredMask ||
                    CountSelectedDays(mask) != daysPerWeek)
                {
                    continue;
                }

                var candidateDays = Enumerable.Range(1, 7)
                    .Where(day => (mask & (1 << (day - 1))) != 0)
                    .ToArray();
                var candidateSeparation = CalculateCircularSeparation(
                    candidateDays);

                if (bestDays is null ||
                    CompareLexicographically(
                        candidateSeparation,
                        bestSeparation!) > 0 ||
                    (CompareLexicographically(
                        candidateSeparation,
                        bestSeparation!) == 0 &&
                     CompareLexicographically(
                         candidateDays,
                         bestDays) < 0))
                {
                    bestDays = candidateDays;
                    bestSeparation = candidateSeparation;
                }
            }

            return bestDays!;
        }

        private static int[] CalculateCircularSeparation(
            IReadOnlyList<int> days)
        {
            var gaps = new int[days.Count];

            for (var index = 0; index < days.Count; index++)
            {
                var current = days[index];
                var next = index == days.Count - 1
                    ? days[0] + 7
                    : days[index + 1];

                gaps[index] = next - current;
            }

            Array.Sort(gaps);
            return gaps;
        }

        private static int CountSelectedDays(int mask)
        {
            var count = 0;

            while (mask != 0)
            {
                count += mask & 1;
                mask >>= 1;
            }

            return count;
        }

        private static int CompareLexicographically(
            IReadOnlyList<int> first,
            IReadOnlyList<int> second)
        {
            for (var index = 0; index < first.Count; index++)
            {
                var comparison = first[index].CompareTo(second[index]);

                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private sealed record SessionDefinition(
            RoutineSessionFocus Focus,
            RoutineSessionVariant Variant);
    }
}
