"use client";

import { ArrowDown, ArrowLeft, ArrowRight, ArrowUp, Check, ChevronDown, ChevronRight, CircleHelp, X } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { formatHistoryGroupDate, formatHistoryTime, getLocalDateKey } from "@/features/history/history-formatters";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getExerciseProgressTrendAction } from "../actions/get-exercise-progress-trend.action";
import { formatCompactProgressPercentage, formatProgressValue } from "../progress-formatters";
import {
  calculateMetricChange,
  formatMetricValue,
  getAvailableMetrics,
  getDefaultMetric,
  getMetricValue,
  getPeriodDescription,
  getPeriodRange,
  progressPeriods,
  type ProgressMetric,
  type ProgressMetricKey,
  type ProgressPeriodKey,
} from "../progress-trend";
import type { ExerciseProgressTrend, ExerciseProgressTrendPoint } from "../types/progress.types";
import { ExerciseProgressChart } from "./exercise-progress-chart";

type LoadStatus = "loading" | "refreshing" | "ready" | "not-found" | "error";

export function ExerciseProgressContent({ exerciseId }: { exerciseId: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [trend, setTrend] = useState<ExerciseProgressTrend | null>(null);
  const [status, setStatus] = useState<LoadStatus>("loading");
  const [period, setPeriod] = useState<ProgressPeriodKey>("3m");
  const [metric, setMetric] = useState<ProgressMetricKey>("e1RmEstimado");
  const [requestKey, setRequestKey] = useState(0);
  const metricDialogRef = useRef<HTMLDialogElement>(null);
  const sessionsDialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    let active = true;

    async function loadTrend() {
      const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
      if (!timeZone) {
        await Promise.resolve();
        if (active) setStatus("error");
        return;
      }

      const result = await getExerciseProgressTrendAction(exerciseId, {
        ...getPeriodRange(period),
        timeZone,
      });
      if (!active) return;

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      if (result.status === "ready") {
        const availableMetrics = getAvailableMetrics(result.trend.points);
        setMetric((current) => availableMetrics.some((item) => item.key === current)
          ? current
          : getDefaultMetric(availableMetrics));
        setTrend(result.trend);
        setStatus("ready");
        return;
      }
      setStatus(result.status);
    }

    void loadTrend().catch(() => {
      if (active) setStatus("error");
    });

    return () => { active = false; };
  }, [exerciseId, invalidateSession, period, requestKey, router]);

  const orderedPoints = useMemo(() => trend
    ? [...trend.points].sort((first, second) => {
      const dateDifference = new Date(first.horaInicio).getTime() - new Date(second.horaInicio).getTime();
      return dateDifference || first.sessionId.localeCompare(second.sessionId);
    })
    : [], [trend]);
  const availableMetrics = useMemo(() => getAvailableMetrics(orderedPoints), [orderedPoints]);

  function changePeriod(nextPeriod: ProgressPeriodKey) {
    if (nextPeriod === period) return;
    setStatus(trend ? "refreshing" : "loading");
    setPeriod(nextPeriod);
  }

  function retry() {
    setStatus(trend ? "refreshing" : "loading");
    setRequestKey((key) => key + 1);
  }

  return (
    <section aria-labelledby="exercise-progress-title" className="mx-auto w-full min-w-0 max-w-xl overflow-x-clip">
      <ProgressHeader name={trend?.nombre} />

      {status === "loading" && !trend ? (
        <ProgressSkeleton />
      ) : status === "not-found" ? (
        <NotFoundState />
      ) : status === "error" ? (
        <div className="mt-8" role="alert">
          <p className="text-base text-text-primary">No pudimos cargar la evolución.</p>
          <Button className="mt-5" onClick={retry} variant="secondary">Reintentar</Button>
        </div>
      ) : trend ? (
        <>
          {orderedPoints.length === 0 ? (
            trend.tieneHistorial && period !== "all"
              ? <EmptyPeriod onViewAll={() => changePeriod("all")} />
              : <EmptyExercise />
          ) : (
            <TrendDetail
              availableMetrics={availableMetrics}
              metric={metric}
              metricDialogRef={metricDialogRef}
              onChangeMetric={setMetric}
              onChangePeriod={changePeriod}
              onOpenMetric={() => metricDialogRef.current?.showModal()}
              onOpenSessions={() => sessionsDialogRef.current?.showModal()}
              period={period}
              points={orderedPoints}
              refreshing={status === "refreshing"}
              sessionsDialogRef={sessionsDialogRef}
            />
          )}
        </>
      ) : null}
    </section>
  );
}

function ProgressHeader({ name }: { name?: string }) {
  const imageSrc = name ? getExerciseImage(name) : undefined;

  return (
    <header>
      <div className="flex min-w-0 items-center gap-1">
        <Link aria-label="Volver a Progreso" className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary" href="/progress">
          <ArrowLeft aria-hidden="true" size={21} />
        </Link>
        <p className="text-sm font-semibold uppercase tracking-[0.1em] text-text-secondary">Progreso</p>
      </div>
      {name ? (
        <div className="mt-2 flex min-w-0 items-center gap-3">
          {imageSrc ? (
            <div aria-hidden="true" className="flex size-18 shrink-0 items-center justify-center rounded-control-sm bg-surface/45 p-1">
              <Image alt="" className="size-full object-contain" height={72} sizes="72px" src={imageSrc} width={72} />
            </div>
          ) : null}
          <h1 className="min-w-0 break-words font-brand text-[1.625rem] font-bold leading-8 text-text-primary" id="exercise-progress-title">{name}</h1>
        </div>
      ) : (
        <h1 className="sr-only" id="exercise-progress-title">Progreso del ejercicio</h1>
      )}
    </header>
  );
}

function TrendDetail({
  availableMetrics,
  metric,
  metricDialogRef,
  onChangeMetric,
  onChangePeriod,
  onOpenMetric,
  onOpenSessions,
  period,
  points,
  refreshing,
  sessionsDialogRef,
}: {
  availableMetrics: ProgressMetric[];
  metric: ProgressMetricKey;
  metricDialogRef: React.RefObject<HTMLDialogElement | null>;
  onChangeMetric: (metric: ProgressMetricKey) => void;
  onChangePeriod: (period: ProgressPeriodKey) => void;
  onOpenMetric: () => void;
  onOpenSessions: () => void;
  period: ProgressPeriodKey;
  points: ExerciseProgressTrendPoint[];
  refreshing: boolean;
  sessionsDialogRef: React.RefObject<HTMLDialogElement | null>;
}) {
  const selectedMetric = availableMetrics.find((item) => item.key === metric) ?? availableMetrics[0];
  const validPoints = selectedMetric
    ? points.filter((point) => getMetricValue(point, selectedMetric.key) !== null)
    : [];
  const values = selectedMetric
    ? validPoints.map((point) => getMetricValue(point, selectedMetric.key)!).filter((value) => value > 0)
    : [];
  const currentValue = values.at(-1);
  const change = calculateMetricChange(values);

  if (!selectedMetric || currentValue === undefined) return null;

  return (
    <div className="mt-5 min-w-0">
      <button className="flex min-h-12 w-full items-center justify-between gap-3 rounded-control-sm border border-border/60 bg-surface/45 px-4 text-left text-base font-semibold text-text-primary transition-colors hover:border-border-strong hover:bg-surface focus-visible:outline-primary" onClick={onOpenMetric} type="button">
        <span>{selectedMetric.label}</span>
        <ChevronDown aria-hidden="true" className="shrink-0 text-primary" size={20} />
      </button>

      <div aria-live="polite" className="mt-6">
        <p className="font-brand text-4xl font-bold leading-none tabular-nums text-text-primary">{formatMetricValue(selectedMetric.key, currentValue)}</p>
        {change === null ? (
          <p className="mt-3 text-base leading-6 text-text-secondary">Necesitás otro registro para comparar.</p>
        ) : (
          <MetricChange change={change} period={period} />
        )}
      </div>

      <div className={refreshing ? "opacity-55 transition-opacity" : "transition-opacity"}>
        <ExerciseProgressChart key={`${period}-${selectedMetric.key}-${points.at(-1)?.sessionId}`} metric={selectedMetric.key} metricLabel={selectedMetric.label} points={points} />
      </div>
      {refreshing ? <p aria-live="polite" className="sr-only">Actualizando evolución.</p> : null}

      <PeriodSelector onChange={onChangePeriod} selected={period} />

      <button className="mt-6 flex min-h-12 w-full items-center justify-between border-t border-border/55 pt-5 text-left text-base font-semibold text-text-secondary transition-colors hover:text-text-primary focus-visible:outline-primary" onClick={onOpenSessions} type="button">
        <span>Ver sesiones</span>
        <ChevronRight aria-hidden="true" size={20} />
      </button>

      <MetricDialog dialogRef={metricDialogRef} metrics={availableMetrics} onSelect={onChangeMetric} selected={selectedMetric.key} />
      <SessionsDialog dialogRef={sessionsDialogRef} points={points} />
    </div>
  );
}

function MetricChange({ change, period }: { change: number; period: ProgressPeriodKey }) {
  const Icon = change > 0 ? ArrowUp : change < 0 ? ArrowDown : ArrowRight;
  const direction = change > 0 ? "aumentó" : change < 0 ? "disminuyó" : "no cambió";
  const value = formatCompactProgressPercentage(Math.abs(change));
  return (
    <p aria-label={`La métrica ${direction} ${value} por ciento en ${getPeriodDescription(period)}.`} className="mt-3 flex items-center gap-2 text-base font-semibold text-info">
      <Icon aria-hidden="true" size={19} strokeWidth={2.25} />
      <span className="tabular-nums">{value}%</span>
      <span className="font-medium text-text-secondary">en {getPeriodDescription(period)}</span>
    </p>
  );
}

function PeriodSelector({ onChange, selected }: { onChange: (period: ProgressPeriodKey) => void; selected: ProgressPeriodKey }) {
  return (
    <div aria-label="Período de evolución" className="mt-3 grid grid-cols-5 gap-1 rounded-full border border-border/55 bg-surface/40 p-1" role="group">
      {progressPeriods.map((period) => (
        <button aria-pressed={selected === period.key} className={`min-h-11 rounded-full px-1 text-sm font-semibold transition-colors focus-visible:outline-primary ${selected === period.key ? "bg-primary/16 text-primary" : "text-text-secondary hover:text-text-primary"}`} key={period.key} onClick={() => onChange(period.key)} type="button">
          {period.label}
        </button>
      ))}
    </div>
  );
}

function MetricDialog({ dialogRef, metrics, onSelect, selected }: {
  dialogRef: React.RefObject<HTMLDialogElement | null>;
  metrics: ProgressMetric[];
  onSelect: (metric: ProgressMetricKey) => void;
  selected: ProgressMetricKey;
}) {
  const [expandedMetric, setExpandedMetric] = useState<ProgressMetricKey | null>(null);

  return (
    <dialog aria-labelledby="metric-dialog-title" aria-modal="true" className="mt-auto max-h-[85dvh] w-full max-w-full rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container" onClick={(event) => { if (event.target === event.currentTarget) event.currentTarget.close(); }} onClose={() => setExpandedMetric(null)} ref={dialogRef}>
      <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
      <div className="max-h-[calc(85dvh-0.75rem)] overflow-y-auto px-4 pb-[calc(1.25rem+env(safe-area-inset-bottom))] pt-3 sm:px-6 sm:pb-6 sm:pt-5">
        <header className="flex items-center gap-3">
          <h2 className="min-w-0 flex-1 font-brand text-xl font-bold text-text-primary" id="metric-dialog-title">Seleccioná una métrica</h2>
          <button aria-label="Cerrar selector de métrica" className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary hover:bg-surface hover:text-text-primary focus-visible:outline-primary" onClick={() => dialogRef.current?.close()} type="button"><X aria-hidden="true" size={20} /></button>
        </header>
        <div className="mt-4 divide-y divide-border/50 border-y border-border/50">
          {metrics.map((metric) => {
            const descriptionId = `metric-description-${metric.key}`;
            const expanded = expandedMetric === metric.key;
            return (
              <div key={metric.key}>
                <div className="grid grid-cols-[minmax(0,1fr)_2.75rem_2.25rem] items-center">
                  <button aria-pressed={selected === metric.key} className="min-h-14 min-w-0 py-3 pr-2 text-left text-base font-semibold text-text-primary hover:text-primary focus-visible:outline-primary" onClick={() => { onSelect(metric.key); dialogRef.current?.close(); }} type="button">
                    {metric.sheetLabel}
                  </button>
                  <button aria-controls={descriptionId} aria-expanded={expanded} aria-label={`Qué significa ${metric.sheetLabel}`} className="flex size-11 items-center justify-center rounded-full text-text-secondary hover:bg-surface hover:text-primary focus-visible:outline-primary" onClick={() => setExpandedMetric(expanded ? null : metric.key)} type="button">
                    <CircleHelp aria-hidden="true" size={19} />
                  </button>
                  <span aria-hidden="true" className="flex size-9 items-center justify-center text-primary">
                    {selected === metric.key ? <Check size={21} /> : null}
                  </span>
                </div>
                {expanded ? (
                  <p className="pb-4 pr-12 text-sm leading-5 text-text-secondary" id={descriptionId}>{metric.description}</p>
                ) : null}
              </div>
            );
          })}
        </div>
      </div>
    </dialog>
  );
}

function SessionsDialog({ dialogRef, points }: { dialogRef: React.RefObject<HTMLDialogElement | null>; points: ExerciseProgressTrendPoint[] }) {
  const groups = useMemo(() => groupSessionPoints(points), [points]);

  return (
    <dialog aria-labelledby="progress-sessions-title" aria-modal="true" className="mt-auto max-h-[75dvh] w-full max-w-full overflow-hidden rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container" onClick={(event) => { if (event.target === event.currentTarget) event.currentTarget.close(); }} ref={dialogRef}>
      <div className="flex max-h-[75dvh] flex-col overflow-hidden sm:max-h-[80dvh]">
        <div className="mx-auto mt-2 h-1 w-10 shrink-0 rounded-full bg-border-strong sm:hidden" />
        <header className="flex shrink-0 items-center gap-3 px-4 pb-3 pt-3 sm:px-6 sm:pt-5">
          <h2 className="min-w-0 flex-1 font-brand text-xl font-bold text-text-primary" id="progress-sessions-title">Sesiones del período · {points.length}</h2>
          <button aria-label="Cerrar sesiones" className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary hover:bg-surface hover:text-text-primary focus-visible:outline-primary" onClick={() => dialogRef.current?.close()} type="button"><X aria-hidden="true" size={20} /></button>
        </header>
        <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-4 pb-[calc(1.25rem+env(safe-area-inset-bottom))] sm:px-6 sm:pb-6">
          {groups.map((group, groupIndex) => (
            <section className={groupIndex === 0 ? "" : "mt-4 border-t border-border/50 pt-4"} key={group.key}>
              <h3 className="text-xs font-semibold uppercase tracking-[0.1em] text-text-secondary">{group.label}</h3>
              <ul className="mt-1 divide-y divide-border/45">
                {group.points.map((point) => (
                  <li key={point.sessionId}>
                    <Link className="group flex min-h-14 items-center gap-3 py-2 focus-visible:outline-primary" href={`/history/${point.sessionId}`}>
                      <time className="w-12 shrink-0 font-brand text-base font-bold tabular-nums text-text-secondary" dateTime={point.horaInicio}>{formatHistoryTime(point.horaInicio) ?? "—"}</time>
                      <span className="min-w-0 flex-1 font-brand text-base font-bold text-text-primary">{formatSessionResult(point)}</span>
                      <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary group-hover:text-primary" size={19} />
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      </div>
    </dialog>
  );
}

function groupSessionPoints(points: ExerciseProgressTrendPoint[]) {
  const groups = new Map<string, { key: string; label: string; points: ExerciseProgressTrendPoint[] }>();
  const orderedPoints = [...points].sort((first, second) => {
    const dateDifference = new Date(second.horaInicio).getTime() - new Date(first.horaInicio).getTime();
    return dateDifference || second.sessionId.localeCompare(first.sessionId);
  });

  for (const point of orderedPoints) {
    const key = getLocalDateKey(point.horaInicio);
    const group = groups.get(key) ?? {
      key,
      label: formatHistoryGroupDate(point.horaInicio),
      points: [],
    };
    group.points.push(point);
    groups.set(key, group);
  }

  return [...groups.values()];
}

function formatSessionResult(point: ExerciseProgressTrendPoint): string {
  if (point.mejorSerie && point.mejorSerie.peso > 0) {
    return `${formatProgressValue(point.mejorSerie.peso)} kg × ${point.mejorSerie.repeticiones}`;
  }
  return `${point.repeticionesMaximas} ${point.repeticionesMaximas === 1 ? "rep" : "reps"}`;
}

function EmptyExercise() {
  return (
    <div className="mt-10 border-t border-border/60 pt-7">
      <h2 className="font-brand text-2xl font-bold leading-8 text-text-primary">Todavía no registraste este ejercicio.</h2>
      <p className="mt-3 max-w-md text-base leading-6 text-text-secondary">Cuando lo entrenes, vas a ver su evolución acá.</p>
      <Link className="mt-6 inline-flex min-h-12 items-center gap-2 rounded-control-sm bg-primary-strong px-4 text-base font-semibold text-text-primary hover:bg-primary focus-visible:outline-primary" href="/routines">Ver rutinas<ChevronRight aria-hidden="true" size={19} /></Link>
    </div>
  );
}

function EmptyPeriod({ onViewAll }: { onViewAll: () => void }) {
  return (
    <div className="mt-10 border-t border-border/60 pt-7">
      <h2 className="font-brand text-2xl font-bold leading-8 text-text-primary">No hay registros en este período.</h2>
      <button className="mt-5 flex min-h-12 items-center text-base font-semibold text-primary hover:text-text-primary focus-visible:outline-primary" onClick={onViewAll} type="button">Ver todo</button>
    </div>
  );
}

function NotFoundState() {
  return (
    <div className="mt-8">
      <p className="text-base text-text-primary">No encontramos este ejercicio.</p>
      <Link className="mt-4 inline-flex min-h-12 items-center gap-1 text-base font-semibold text-primary focus-visible:outline-primary" href="/progress">Volver a Progreso<ChevronRight aria-hidden="true" size={18} /></Link>
    </div>
  );
}

function ProgressSkeleton() {
  return (
    <div aria-label="Cargando evolución" className="mt-5 animate-pulse" role="status">
      <div className="h-8 w-3/4 max-w-80 rounded bg-surface-elevated" />
      <div className="mt-7 h-12 w-full rounded-control-sm bg-surface-elevated" />
      <div className="mt-6 h-10 w-40 rounded bg-surface-elevated" />
      <div className="mt-3 h-5 w-52 rounded bg-surface-elevated" />
      <div className="mt-4 h-[14.375rem] w-full rounded-control-sm bg-surface/55" />
      <div className="mt-3 h-12 w-full rounded-full bg-surface-elevated" />
    </div>
  );
}
