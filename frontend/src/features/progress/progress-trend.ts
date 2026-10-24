import type { ExerciseProgressTrendPoint } from "./types/progress.types";

export type ProgressMetricKey =
  | "e1RmEstimado"
  | "pesoMaximo"
  | "repeticionesMaximas"
  | "volumenSesion"
  | "volumenMaximoSerie"
  | "seriesCompletadas"
  | "repeticionesTotales";

export type ProgressPeriodKey = "1m" | "3m" | "6m" | "1y" | "all";

export type ProgressMetric = {
  key: ProgressMetricKey;
  label: string;
  sheetLabel: string;
  description: string;
};

export const progressMetrics: ProgressMetric[] = [
  {
    key: "e1RmEstimado",
    label: "1RM estimado",
    sheetLabel: "1RM estimado",
    description: "Peso aproximado que podrías mover en una repetición.",
  },
  {
    key: "pesoMaximo",
    label: "Peso máximo",
    sheetLabel: "Peso máximo",
    description: "Mayor peso utilizado en una serie de esa sesión.",
  },
  {
    key: "repeticionesMaximas",
    label: "Repeticiones máximas",
    sheetLabel: "Repeticiones máximas",
    description: "Mayor cantidad de repeticiones realizadas en una serie.",
  },
  {
    key: "volumenSesion",
    label: "Volumen por sesión",
    sheetLabel: "Volumen por sesión",
    description: "Suma del peso × repeticiones de todas las series.",
  },
  {
    key: "volumenMaximoSerie",
    label: "Volumen máx. por serie",
    sheetLabel: "Volumen máx. de una serie",
    description: "Mayor peso × repeticiones conseguido en una sola serie.",
  },
  {
    key: "seriesCompletadas",
    label: "Series completadas",
    sheetLabel: "Series completadas",
    description: "Cantidad de series registradas en la sesión.",
  },
  {
    key: "repeticionesTotales",
    label: "Repeticiones totales",
    sheetLabel: "Repeticiones totales",
    description: "Suma de todas las repeticiones realizadas en la sesión.",
  },
];

export const progressPeriods: Array<{ key: ProgressPeriodKey; label: string }> = [
  { key: "1m", label: "1M" },
  { key: "3m", label: "3M" },
  { key: "6m", label: "6M" },
  { key: "1y", label: "1A" },
  { key: "all", label: "Todo" },
];

const decimalFormatter = new Intl.NumberFormat("es-AR", { maximumFractionDigits: 2 });
const volumeFormatter = new Intl.NumberFormat("es-AR", { maximumFractionDigits: 0 });

export function getMetricValue(point: ExerciseProgressTrendPoint, metric: ProgressMetricKey): number | null {
  return point[metric];
}

export function getAvailableMetrics(points: ExerciseProgressTrendPoint[]): ProgressMetric[] {
  return progressMetrics.filter((metric) =>
    points.some((point) => {
      const value = getMetricValue(point, metric.key);
      return value !== null && value > 0;
    }),
  );
}

export function getDefaultMetric(metrics: ProgressMetric[]): ProgressMetricKey {
  if (metrics.some((metric) => metric.key === "e1RmEstimado")) return "e1RmEstimado";
  if (metrics.some((metric) => metric.key === "repeticionesMaximas")) return "repeticionesMaximas";
  return metrics[0]?.key ?? "repeticionesMaximas";
}

export function formatMetricValue(metric: ProgressMetricKey, value: number): string {
  if (metric === "e1RmEstimado" || metric === "pesoMaximo") {
    return `${decimalFormatter.format(value)} kg`;
  }
  if (metric === "volumenSesion" || metric === "volumenMaximoSerie") {
    return `${volumeFormatter.format(value)} kg`;
  }
  if (metric === "seriesCompletadas") {
    return `${volumeFormatter.format(value)} ${value === 1 ? "serie" : "series"}`;
  }
  return `${volumeFormatter.format(value)} ${value === 1 ? "rep" : "reps"}`;
}

export function calculateMetricChange(values: number[]): number | null {
  if (values.length < 2 || values[0] === 0) return null;
  return ((values.at(-1)! - values[0]) / values[0]) * 100;
}

export function getPeriodRange(period: ProgressPeriodKey, today = new Date()): { from?: string; to?: string } {
  if (period === "all") return {};

  const to = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const from = period === "1y"
    ? subtractCalendarYears(to, 1)
    : subtractCalendarMonths(to, period === "1m" ? 1 : period === "3m" ? 3 : 6);

  return { from: toLocalDateKey(from), to: toLocalDateKey(to) };
}

export function getPeriodDescription(period: ProgressPeriodKey): string {
  if (period === "1m") return "1 mes";
  if (period === "3m") return "3 meses";
  if (period === "6m") return "6 meses";
  if (period === "1y") return "1 año";
  return "todo el historial";
}

function subtractCalendarMonths(date: Date, months: number): Date {
  const target = new Date(date.getFullYear(), date.getMonth() - months, 1);
  const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
  target.setDate(Math.min(date.getDate(), lastDay));
  return target;
}

function subtractCalendarYears(date: Date, years: number): Date {
  const targetYear = date.getFullYear() - years;
  const target = new Date(targetYear, date.getMonth(), 1);
  const lastDay = new Date(targetYear, date.getMonth() + 1, 0).getDate();
  target.setDate(Math.min(date.getDate(), lastDay));
  return target;
}

function toLocalDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}
