"use client";

import { useState } from "react";
import { formatShortProgressDate } from "../progress-formatters";
import { formatMetricValue, getMetricValue, type ProgressMetricKey } from "../progress-trend";
import type { ExerciseProgressTrendPoint } from "../types/progress.types";

type ChartPoint = {
  sessionId: string;
  date: string;
  value: number;
};

const WIDTH = 360;
const HEIGHT = 230;
const HORIZONTAL_PADDING = 24;
const TOP_PADDING = 40;
const BOTTOM_PADDING = 38;
const DOMAIN_PADDING_RATIO = 0.1;

export function ExerciseProgressChart({ metric, metricLabel, points }: {
  metric: ProgressMetricKey;
  metricLabel: string;
  points: ExerciseProgressTrendPoint[];
}) {
  const chartPoints: ChartPoint[] = points.flatMap((point) => {
    const value = getMetricValue(point, metric);
    return value === null ? [] : [{ sessionId: point.sessionId, date: point.horaInicio, value }];
  });
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null);

  if (chartPoints.length === 0) return null;

  const selectedPoint = chartPoints.find((point) => point.sessionId === selectedSessionId) ?? chartPoints.at(-1)!;
  const values = chartPoints.map((point) => point.value);
  const minimum = Math.min(...values);
  const maximum = Math.max(...values);
  const rawRange = maximum - minimum;
  const domainPadding = rawRange > 0 ? rawRange * DOMAIN_PADDING_RATIO : Math.max(Math.abs(maximum) * 0.05, 1);
  const domainMinimum = minimum - domainPadding;
  const domainMaximum = maximum + domainPadding;
  const domainRange = domainMaximum - domainMinimum;
  const chartHeight = HEIGHT - TOP_PADDING - BOTTOM_PADDING;
  const chartWidth = WIDTH - HORIZONTAL_PADDING * 2;
  const coordinates = chartPoints.map((point, index) => {
    const x = chartPoints.length === 1 ? WIDTH / 2 : HORIZONTAL_PADDING + (index / (chartPoints.length - 1)) * chartWidth;
    const normalizedValue = (point.value - domainMinimum) / domainRange;
    const y = TOP_PADDING + (1 - normalizedValue) * chartHeight;
    return { ...point, x, y };
  });
  const description = chartPoints.length === 1
    ? `${metricLabel}: ${formatMetricValue(metric, chartPoints[0].value)} en ${formatShortProgressDate(chartPoints[0].date)}.`
    : `${metricLabel} pasó de ${formatMetricValue(metric, chartPoints[0].value)} a ${formatMetricValue(metric, chartPoints.at(-1)!.value)} en el período.`;

  return (
    <div className="relative mt-4 min-w-0" key={metric}>
      <div aria-live="polite" className="pointer-events-none absolute top-0 right-0 z-10 rounded-control-sm border border-border/70 bg-surface-elevated/95 px-3 py-2 text-right shadow-elevated">
        <p className="font-brand text-base font-bold tabular-nums text-text-primary">{formatMetricValue(metric, selectedPoint.value)}</p>
        <p className="mt-0.5 text-xs font-medium text-text-secondary">{formatShortProgressDate(selectedPoint.date)}</p>
      </div>

      <svg aria-label={description} className="h-[14.375rem] w-full max-w-full overflow-visible" preserveAspectRatio="xMidYMid meet" role="group" viewBox={`0 0 ${WIDTH} ${HEIGHT}`}>
        <title>{description}</title>
        <line className="stroke-border/55" strokeWidth="1" x1={HORIZONTAL_PADDING} x2={WIDTH - HORIZONTAL_PADDING} y1={HEIGHT - BOTTOM_PADDING} y2={HEIGHT - BOTTOM_PADDING} />
        {coordinates.length > 1 ? (
          <polyline className="fill-none stroke-info" points={coordinates.map((point) => `${point.x},${point.y}`).join(" ")} strokeLinecap="round" strokeLinejoin="round" strokeWidth="2.5" vectorEffect="non-scaling-stroke" />
        ) : null}
        {coordinates.map((point) => {
          const selected = point.sessionId === selectedPoint.sessionId;
          const pointLabel = `${formatMetricValue(metric, point.value)}, ${formatShortProgressDate(point.date)}`;
          return (
            <g key={point.sessionId}>
              <circle aria-hidden="true" className={selected ? "fill-primary stroke-background" : "fill-background stroke-primary"} cx={point.x} cy={point.y} r={selected ? 5 : 4} strokeWidth="2.5" vectorEffect="non-scaling-stroke" />
              <circle aria-label={pointLabel} className="cursor-pointer fill-transparent focus:outline-none focus-visible:stroke-primary" cx={point.x} cy={point.y} onClick={() => setSelectedSessionId(point.sessionId)} onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  setSelectedSessionId(point.sessionId);
                }
              }} r="22" role="button" strokeWidth="2" tabIndex={0} />
            </g>
          );
        })}
        <text className="fill-text-secondary" fontSize="12" fontWeight="600" x={HORIZONTAL_PADDING} y={HEIGHT - 8}>{formatShortProgressDate(chartPoints[0].date)}</text>
        {chartPoints.length > 1 ? (
          <text className="fill-text-secondary" fontSize="12" fontWeight="600" textAnchor="end" x={WIDTH - HORIZONTAL_PADDING} y={HEIGHT - 8}>{formatShortProgressDate(chartPoints.at(-1)!.date)}</text>
        ) : null}
      </svg>
      <p className="sr-only">{description}</p>
    </div>
  );
}
