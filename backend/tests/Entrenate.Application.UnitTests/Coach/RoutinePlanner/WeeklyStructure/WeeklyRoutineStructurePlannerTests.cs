using Entrenate.Application.Coach.RoutinePlanner.WeeklyStructure;
using Entrenate.Domain.Enums;
using Xunit;

namespace Entrenate.Application.UnitTests.Coach.RoutinePlanner.WeeklyStructure
{
    public class WeeklyRoutineStructurePlannerTests
    {
        public static TheoryData<
            int,
            RoutineSessionFocus[],
            RoutineSessionVariant[]> SplitCases => new()
            {
                {
                    1,
                    [RoutineSessionFocus.FullBody],
                    [RoutineSessionVariant.A]
                },
                {
                    2,
                    [
                        RoutineSessionFocus.FullBody,
                        RoutineSessionFocus.FullBody
                    ],
                    [
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.B
                    ]
                },
                {
                    3,
                    [
                        RoutineSessionFocus.FullBody,
                        RoutineSessionFocus.FullBody,
                        RoutineSessionFocus.FullBody
                    ],
                    [
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.C
                    ]
                },
                {
                    4,
                    [
                        RoutineSessionFocus.Upper,
                        RoutineSessionFocus.Lower,
                        RoutineSessionFocus.Upper,
                        RoutineSessionFocus.Lower
                    ],
                    [
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.B
                    ]
                },
                {
                    5,
                    [
                        RoutineSessionFocus.Upper,
                        RoutineSessionFocus.Lower,
                        RoutineSessionFocus.Push,
                        RoutineSessionFocus.Pull,
                        RoutineSessionFocus.Legs
                    ],
                    Enumerable.Repeat(
                        RoutineSessionVariant.None,
                        5).ToArray()
                },
                {
                    6,
                    [
                        RoutineSessionFocus.Push,
                        RoutineSessionFocus.Pull,
                        RoutineSessionFocus.Legs,
                        RoutineSessionFocus.Push,
                        RoutineSessionFocus.Pull,
                        RoutineSessionFocus.Legs
                    ],
                    [
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.B
                    ]
                },
                {
                    7,
                    [
                        RoutineSessionFocus.Push,
                        RoutineSessionFocus.Pull,
                        RoutineSessionFocus.Legs,
                        RoutineSessionFocus.Push,
                        RoutineSessionFocus.Pull,
                        RoutineSessionFocus.Legs,
                        RoutineSessionFocus.CoreCalves
                    ],
                    [
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.A,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.B,
                        RoutineSessionVariant.None
                    ]
                }
            };

        [Theory]
        [MemberData(nameof(SplitCases))]
        public void Build_AssignsNormativeSplitForRequestedDays(
            int daysPerWeek,
            RoutineSessionFocus[] expectedFocuses,
            RoutineSessionVariant[] expectedVariants)
        {
            var result = WeeklyRoutineStructurePlanner.Build(
                daysPerWeek,
                []);

            Assert.Equal(daysPerWeek, result.DaysPerWeek);
            Assert.Equal(
                Enumerable.Range(1, daysPerWeek),
                result.Days.Select(day => day.Order));
            Assert.Equal(
                expectedFocuses,
                result.Days.Select(day => day.Focus));
            Assert.Equal(
                expectedVariants,
                result.Days.Select(day => day.Variant));
            Assert.Equal(
                daysPerWeek,
                result.Days.Select(day => day.ScheduledDay).Distinct().Count());
            Assert.All(
                result.Days,
                day => Assert.Equal(
                    ScheduledDayOrigin.Derived,
                    day.DayOrigin));
        }

        [Fact]
        public void Build_PreservesCompletePreferredCalendarInWeeklyOrder()
        {
            var result = WeeklyRoutineStructurePlanner.Build(
                3,
                [
                    DiaSemana.Sabado,
                    DiaSemana.Martes,
                    DiaSemana.Jueves
                ]);

            Assert.Equal(
                [
                    DiaSemana.Martes,
                    DiaSemana.Jueves,
                    DiaSemana.Sabado
                ],
                result.Days.Select(day => day.ScheduledDay));
            Assert.All(
                result.Days,
                day => Assert.Equal(
                    ScheduledDayOrigin.Preferred,
                    day.DayOrigin));
        }

        [Fact]
        public void Build_DerivesMissingDaysWithMaximumCircularSeparation()
        {
            var result = WeeklyRoutineStructurePlanner.Build(
                4,
                [DiaSemana.Martes, DiaSemana.Sabado]);

            Assert.Equal(
                [
                    DiaSemana.Lunes,
                    DiaSemana.Martes,
                    DiaSemana.Jueves,
                    DiaSemana.Sabado
                ],
                result.Days.Select(day => day.ScheduledDay));
            Assert.Equal(
                [
                    ScheduledDayOrigin.Derived,
                    ScheduledDayOrigin.Preferred,
                    ScheduledDayOrigin.Derived,
                    ScheduledDayOrigin.Preferred
                ],
                result.Days.Select(day => day.DayOrigin));
        }

        [Fact]
        public void Build_ReturnsSameStructureForSameInput()
        {
            var preferredDays = new[]
            {
                DiaSemana.Miercoles,
                DiaSemana.Domingo
            };

            var first = WeeklyRoutineStructurePlanner.Build(
                5,
                preferredDays);
            var second = WeeklyRoutineStructurePlanner.Build(
                5,
                preferredDays);

            Assert.Equal(first.Days, second.Days);
        }
    }
}
