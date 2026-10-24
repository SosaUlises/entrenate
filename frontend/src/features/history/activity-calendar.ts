export type CalendarDay = {
  date: Date;
  key: string;
  dayNumber: number;
};

export type YearWeek = Array<CalendarDay | null>;

export function toLocalDateKey(date: Date): string {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, "0"),
    String(date.getDate()).padStart(2, "0"),
  ].join("-");
}

export function parseLocalDateKey(key: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(key);
  if (!match) return null;

  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
  return Number.isNaN(date.getTime()) ? null : date;
}

export function getMonthRange(month: Date): { from: string; to: string } {
  const year = month.getFullYear();
  const monthIndex = month.getMonth();
  return {
    from: toLocalDateKey(new Date(year, monthIndex, 1)),
    to: toLocalDateKey(new Date(year, monthIndex + 1, 0)),
  };
}

export function getYearRange(year: number): { from: string; to: string } {
  return {
    from: `${year}-01-01`,
    to: `${year}-12-31`,
  };
}

export function buildMonthCells(month: Date): Array<CalendarDay | null> {
  const year = month.getFullYear();
  const monthIndex = month.getMonth();
  const leadingEmptyCells = mondayIndex(new Date(year, monthIndex, 1));
  const daysInMonth = new Date(year, monthIndex + 1, 0).getDate();
  const totalCells = Math.ceil((leadingEmptyCells + daysInMonth) / 7) * 7;

  return Array.from({ length: totalCells }, (_, index) => {
    const dayNumber = index - leadingEmptyCells + 1;
    if (dayNumber < 1 || dayNumber > daysInMonth) return null;

    const date = new Date(year, monthIndex, dayNumber);
    return { date, dayNumber, key: toLocalDateKey(date) };
  });
}

export function buildYearWeeks(year: number): YearWeek[] {
  const firstDay = new Date(year, 0, 1);
  const lastDay = new Date(year, 11, 31);
  const gridStart = addDays(firstDay, -mondayIndex(firstDay));
  const gridEnd = addDays(lastDay, 6 - mondayIndex(lastDay));
  const dayCount = Math.round((gridEnd.getTime() - gridStart.getTime()) / 86_400_000) + 1;
  const weekCount = dayCount / 7;

  return Array.from({ length: weekCount }, (_, weekIndex) =>
    Array.from({ length: 7 }, (_, dayIndex) => {
      const date = addDays(gridStart, weekIndex * 7 + dayIndex);
      if (date.getFullYear() !== year) return null;
      return { date, dayNumber: date.getDate(), key: toLocalDateKey(date) };
    }),
  );
}

export function getMonthLabel(month: Date): string {
  const value = new Intl.DateTimeFormat("es-AR", {
    month: "long",
    year: "numeric",
  }).format(month);
  return value.charAt(0).toLocaleUpperCase("es-AR") + value.slice(1);
}

export function getShortMonthLabel(date: Date): string {
  return new Intl.DateTimeFormat("es-AR", { month: "short" })
    .format(date)
    .replaceAll(".", "")
    .toLocaleUpperCase("es-AR");
}

export function formatAccessibleDate(date: Date): string {
  return new Intl.DateTimeFormat("es-AR", {
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(date);
}

function mondayIndex(date: Date): number {
  return (date.getDay() + 6) % 7;
}

function addDays(date: Date, amount: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + amount);
}
