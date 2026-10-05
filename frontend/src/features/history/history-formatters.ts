const listDateFormatter = new Intl.DateTimeFormat("es-AR", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

const detailDateFormatter = new Intl.DateTimeFormat("es-AR", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

const timeFormatter = new Intl.DateTimeFormat("es-AR", {
  hour: "2-digit",
  hour12: false,
  minute: "2-digit",
});

const decimalFormatter = new Intl.NumberFormat("es-AR", {
  maximumFractionDigits: 2,
});

function parseDate(value: string | null): Date | null {
  if (!value) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

function withoutMonthPeriod(value: string): string {
  return value.replaceAll(".", "");
}

export function getLocalDateKey(value: string): string {
  const date = parseDate(value);
  if (!date) return "invalid-date";

  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, "0"),
    String(date.getDate()).padStart(2, "0"),
  ].join("-");
}

export function formatHistoryGroupDate(value: string): string {
  const date = parseDate(value);
  if (!date) return "FECHA NO DISPONIBLE";
  return withoutMonthPeriod(listDateFormatter.format(date)).toLocaleUpperCase("es-AR");
}

export function formatHistoryDetailDate(value: string): string | null {
  const date = parseDate(value);
  return date ? withoutMonthPeriod(detailDateFormatter.format(date)) : null;
}

export function formatHistoryTime(value: string): string | null {
  const date = parseDate(value);
  return date ? timeFormatter.format(date) : null;
}

export function formatDuration(startValue: string, endValue: string | null): string | null {
  const start = parseDate(startValue);
  const end = parseDate(endValue);
  if (!start || !end) return null;

  const difference = end.getTime() - start.getTime();
  if (difference < 0) return null;

  const totalMinutes = Math.floor(difference / 60_000);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours === 0) return `${totalMinutes} min`;
  return minutes === 0 ? `${hours} h` : `${hours} h ${minutes} min`;
}

export function formatDecimal(value: number): string {
  return decimalFormatter.format(value);
}

export function formatExerciseCount(value: number): string {
  return `${value} ${value === 1 ? "ejercicio" : "ejercicios"}`;
}

export function formatSetCount(value: number): string {
  return `${value} ${value === 1 ? "serie registrada" : "series registradas"}`;
}
