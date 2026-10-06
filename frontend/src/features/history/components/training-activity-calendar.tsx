"use client";

import { ChevronLeft, ChevronRight, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getTrainingActivityAction } from "../actions/get-training-activity.action";
import {
  buildMonthCells,
  buildYearWeeks,
  formatAccessibleDate,
  getMonthLabel,
  getMonthRange,
  getShortMonthLabel,
  getYearRange,
  parseLocalDateKey,
  toLocalDateKey,
  type CalendarDay,
} from "../activity-calendar";
import {
  formatDuration,
  formatExerciseCount,
  formatHistoryTime,
  formatSetCount,
  getLocalDateKey,
} from "../history-formatters";
import {
  TRAINING_SESSION_STATUS,
  type TrainingActivity,
  type TrainingSessionHistorySummary,
} from "../types/history.types";

type ActivityMode = "month" | "year";
type ActivityState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; activity: TrainingActivity };
type HistoryLoadState = "loading" | "ready" | "error";
type SelectedDay = { key: string; sessionCount: number };

const weekDayLabels = ["L", "M", "X", "J", "V", "S", "D"];

export function TrainingActivityCalendar({
  historyState,
  sessions,
}: {
  historyState: HistoryLoadState;
  sessions: TrainingSessionHistorySummary[];
}) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [today] = useState(() => new Date());
  const [mode, setMode] = useState<ActivityMode>("month");
  const [selectedMonth, setSelectedMonth] = useState(() => new Date(today.getFullYear(), today.getMonth(), 1));
  const [selectedYear, setSelectedYear] = useState(today.getFullYear());
  const [state, setState] = useState<ActivityState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [selectedDay, setSelectedDay] = useState<SelectedDay | null>(null);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const annualScrollRef = useRef<HTMLDivElement>(null);
  const range = useMemo(
    () => mode === "month" ? getMonthRange(selectedMonth) : getYearRange(selectedYear),
    [mode, selectedMonth, selectedYear],
  );
  const todayKey = toLocalDateKey(today);

  useEffect(() => {
    let active = true;

    async function loadActivity() {
      const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
      if (!timeZone) {
        await Promise.resolve();
        if (active) setState({ status: "error" });
        return;
      }

      const result = await getTrainingActivityAction({ ...range, timeZone });
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      setState(result);
    }

    void loadActivity().catch(() => {
      if (active) setState({ status: "error" });
    });

    return () => { active = false; };
  }, [invalidateSession, range, requestKey, router]);

  useEffect(() => {
    if (selectedDay && !dialogRef.current?.open) dialogRef.current?.showModal();
  }, [selectedDay]);

  useEffect(() => {
    if (mode !== "year" || state.status !== "ready") return;
    const frame = requestAnimationFrame(() => {
      const container = annualScrollRef.current;
      if (container) container.scrollLeft = container.scrollWidth - container.clientWidth;
    });
    return () => cancelAnimationFrame(frame);
  }, [mode, selectedYear, state.status]);

  const counts = useMemo(() => {
    if (state.status !== "ready") return new Map<string, number>();
    return new Map(state.activity.days.map((day) => [day.date, day.sessionCount]));
  }, [state]);

  function changeMode(nextMode: ActivityMode) {
    if (nextMode === mode) return;
    setState({ status: "loading" });
    setMode(nextMode);
  }

  function changeMonth(amount: number) {
    setState({ status: "loading" });
    setSelectedMonth((current) => new Date(current.getFullYear(), current.getMonth() + amount, 1));
  }

  function changeYear(amount: number) {
    setState({ status: "loading" });
    setSelectedYear((current) => current + amount);
  }

  function retry() {
    setState({ status: "loading" });
    setRequestKey((current) => current + 1);
  }

  const nextMonthDisabled = selectedMonth.getFullYear() === today.getFullYear() && selectedMonth.getMonth() === today.getMonth();
  const nextYearDisabled = selectedYear >= today.getFullYear();

  return (
    <section aria-labelledby="activity-title" className="mt-8 min-w-0 max-w-full overflow-x-hidden border-b border-border/50 pb-8 sm:mt-10 sm:pb-10">
      <div className="flex min-w-0 max-w-full items-center justify-between gap-4">
        <h2 className="text-xs font-semibold uppercase tracking-[0.14em] text-text-secondary" id="activity-title">Actividad</h2>
        <div aria-label="Vista de actividad" className="inline-flex rounded-full border border-border/50 bg-surface/45 p-1" role="group">
          {(["month", "year"] as const).map((item) => (
            <button
              aria-pressed={mode === item}
              className={`min-h-9 rounded-full px-3.5 text-xs font-semibold transition-colors focus-visible:outline-primary ${mode === item ? "bg-primary/15 text-primary" : "text-text-secondary hover:text-text-primary"}`}
              key={item}
              onClick={() => changeMode(item)}
              type="button"
            >
              {item === "month" ? "Mes" : "Año"}
            </button>
          ))}
        </div>
      </div>

      {state.status === "error" ? (
        <div className="mt-6" role="alert">
          <p className="text-sm text-text-primary">No pudimos cargar tu actividad.</p>
          <Button className="mt-4" onClick={retry} variant="secondary">Reintentar</Button>
        </div>
      ) : mode === "month" ? (
        <MonthView
          activity={state.status === "ready" ? state.activity : null}
          counts={counts}
          loading={state.status === "loading"}
          month={selectedMonth}
          nextDisabled={nextMonthDisabled}
          onNext={() => changeMonth(1)}
          onPrevious={() => changeMonth(-1)}
          onSelectDay={setSelectedDay}
          todayKey={todayKey}
        />
      ) : (
        <YearView
          activity={state.status === "ready" ? state.activity : null}
          counts={counts}
          loading={state.status === "loading"}
          nextDisabled={nextYearDisabled}
          onNext={() => changeYear(1)}
          onPrevious={() => changeYear(-1)}
          onSelectDay={setSelectedDay}
          scrollRef={annualScrollRef}
          todayKey={todayKey}
          year={selectedYear}
        />
      )}

      <DayDetailDialog
        historyState={historyState}
        onClose={() => dialogRef.current?.close()}
        selectedDay={selectedDay}
        sessions={sessions}
        dialogRef={dialogRef}
        onClosed={() => setSelectedDay(null)}
      />
    </section>
  );
}

function MonthView({
  activity,
  counts,
  loading,
  month,
  nextDisabled,
  onNext,
  onPrevious,
  onSelectDay,
  todayKey,
}: {
  activity: TrainingActivity | null;
  counts: Map<string, number>;
  loading: boolean;
  month: Date;
  nextDisabled: boolean;
  onNext: () => void;
  onPrevious: () => void;
  onSelectDay: (day: SelectedDay) => void;
  todayKey: string;
}) {
  const cells = buildMonthCells(month);

  return (
    <div className="mt-4 min-w-0 w-full max-w-md overflow-hidden rounded-container border border-border/55 bg-surface/35 p-3 sm:mt-5 sm:p-5">
      <CalendarNavigation label={getMonthLabel(month)} nextDisabled={nextDisabled} onNext={onNext} onPrevious={onPrevious} />
      <div aria-label={`Actividad de ${getMonthLabel(month)}`} className="mt-3 grid min-w-0 max-w-full grid-cols-7 gap-1.5 sm:mt-4 sm:gap-2" role="grid">
        {weekDayLabels.map((label) => (
          <span className="pb-1 text-center text-xs font-semibold text-text-secondary" key={label} role="columnheader">{label}</span>
        ))}
        {loading
          ? Array.from({ length: cells.length }, (_, index) => <span aria-hidden="true" className="aspect-square animate-pulse rounded-[0.625rem] bg-surface-elevated/55" key={index} />)
          : cells.map((day, index) => day
            ? <MonthDayCell count={counts.get(day.key) ?? 0} day={day} future={day.key > todayKey} key={day.key} onSelect={onSelectDay} />
            : <span aria-hidden="true" key={`empty-${index}`} />)}
      </div>
      {!loading && activity ? <ActivitySummary activity={activity} className="mt-4 border-t border-border/45 pt-3" /> : <div aria-hidden="true" className="mt-4 h-8 border-t border-border/45" />}
    </div>
  );
}

function MonthDayCell({ count, day, future, onSelect }: { count: number; day: CalendarDay; future: boolean; onSelect: (day: SelectedDay) => void }) {
  const label = getDayAriaLabel(day.date, count, future);
  const className = `flex aspect-square min-w-0 items-center justify-center rounded-[0.625rem] border text-sm font-semibold tabular-nums transition-[background-color,border-color,color,transform] ${getActivityClasses(count, future)} ${count > 0 && !future ? "hover:border-primary active:scale-95 focus-visible:outline-primary" : ""}`;

  return count > 0 && !future ? (
    <button aria-label={label} className={className} onClick={() => onSelect({ key: day.key, sessionCount: count })} title={label} type="button">{day.dayNumber}</button>
  ) : (
    <span aria-label={label} className={className} role="gridcell">{day.dayNumber}</span>
  );
}

function YearView({
  activity,
  counts,
  loading,
  nextDisabled,
  onNext,
  onPrevious,
  onSelectDay,
  scrollRef,
  todayKey,
  year,
}: {
  activity: TrainingActivity | null;
  counts: Map<string, number>;
  loading: boolean;
  nextDisabled: boolean;
  onNext: () => void;
  onPrevious: () => void;
  onSelectDay: (day: SelectedDay) => void;
  scrollRef: React.RefObject<HTMLDivElement | null>;
  todayKey: string;
  year: number;
}) {
  const weeks = buildYearWeeks(year);
  const columnTemplate = `repeat(${weeks.length}, 1.5rem)`;

  return (
    <div className="mt-4 min-w-0 w-full max-w-full overflow-hidden rounded-container border border-border/55 bg-surface/35 p-3 sm:mt-5 sm:p-5">
      <CalendarNavigation label={String(year)} nextDisabled={nextDisabled} onNext={onNext} onPrevious={onPrevious} />
      <div className="mt-4 flex min-w-0 max-w-full gap-2 overflow-hidden sm:mt-5">
        <div aria-hidden="true" className="grid shrink-0 grid-rows-7 gap-1 pt-6 text-[0.625rem] font-medium text-text-secondary">
          {weekDayLabels.map((label) => <span className="flex h-6 items-center" key={label}>{label}</span>)}
        </div>
        <div aria-label={`Actividad de ${year}. Deslizá horizontalmente para recorrer el año.`} className="activity-scrollbar w-0 min-w-0 flex-1 overflow-x-auto overscroll-x-contain pb-2" ref={scrollRef} role="region" tabIndex={0}>
          <div className="w-max min-w-full">
            <div aria-hidden="true" className="grid h-6 gap-1" style={{ gridTemplateColumns: columnTemplate }}>
              {weeks.map((week, index) => {
                const monthStart = week.find((day) => day?.dayNumber === 1);
                return <span className="text-[0.625rem] font-semibold text-text-secondary" key={index}>{monthStart ? getShortMonthLabel(monthStart.date) : ""}</span>;
              })}
            </div>
            <div aria-label={`Días de actividad de ${year}`} className="grid grid-flow-col grid-rows-7 gap-1" role="grid" style={{ gridTemplateColumns: columnTemplate }}>
              {loading
                ? Array.from({ length: weeks.length * 7 }, (_, index) => <span aria-hidden="true" className="size-6 animate-pulse rounded-[0.4rem] bg-surface-elevated/55" key={index} />)
                : weeks.flatMap((week, weekIndex) => week.map((day, dayIndex) => day
                  ? <YearDayCell count={counts.get(day.key) ?? 0} day={day} future={day.key > todayKey} key={day.key} onSelect={onSelectDay} />
                  : <span aria-hidden="true" className="size-6" key={`empty-${weekIndex}-${dayIndex}`} />))}
            </div>
          </div>
        </div>
      </div>
      {!loading && activity ? <ActivitySummary activity={activity} className="mt-4 border-t border-border/45 pt-3" /> : <div aria-hidden="true" className="mt-4 h-8 border-t border-border/45" />}
    </div>
  );
}

function YearDayCell({ count, day, future, onSelect }: { count: number; day: CalendarDay; future: boolean; onSelect: (day: SelectedDay) => void }) {
  const label = getDayAriaLabel(day.date, count, future);
  const className = `size-6 rounded-[0.4rem] border transition-[background-color,border-color,transform] ${getActivityClasses(count, future)} ${count > 0 && !future ? "hover:border-primary active:scale-90 focus-visible:outline-primary" : ""}`;

  return count > 0 && !future ? (
    <button aria-label={label} className={className} onClick={() => onSelect({ key: day.key, sessionCount: count })} title={label} type="button" />
  ) : (
    <span aria-label={label} className={className} role="gridcell" title={label} />
  );
}

function CalendarNavigation({ label, nextDisabled, onNext, onPrevious }: { label: string; nextDisabled: boolean; onNext: () => void; onPrevious: () => void }) {
  return (
    <div className="flex min-h-11 items-center justify-between gap-3">
      <button aria-label="Período anterior" className="flex size-11 items-center justify-center rounded-control-sm text-text-secondary hover:bg-surface hover:text-primary focus-visible:outline-primary" onClick={onPrevious} type="button"><ChevronLeft aria-hidden="true" size={20} /></button>
      <p aria-live="polite" className="font-brand text-xl font-bold leading-7 text-text-primary">{label}</p>
      <button aria-label="Período siguiente" className="flex size-11 items-center justify-center rounded-control-sm text-text-secondary hover:bg-surface hover:text-primary focus-visible:outline-primary disabled:cursor-not-allowed disabled:opacity-30" disabled={nextDisabled} onClick={onNext} type="button"><ChevronRight aria-hidden="true" size={20} /></button>
    </div>
  );
}

function ActivitySummary({ activity, className }: { activity: TrainingActivity; className?: string }) {
  return (
    <p className={`${className ?? ""} text-sm leading-5 text-text-secondary`}>
      <span className="font-brand font-bold tabular-nums text-text-primary">{activity.totalSessions}</span> {activity.totalSessions === 1 ? "entrenamiento" : "entrenamientos"}
      <span aria-hidden="true"> · </span>
      <span className="font-brand font-bold tabular-nums text-text-primary">{activity.activeDays}</span> {activity.activeDays === 1 ? "día activo" : "días activos"}
    </p>
  );
}

function DayDetailDialog({
  dialogRef,
  historyState,
  onClose,
  onClosed,
  selectedDay,
  sessions,
}: {
  dialogRef: React.RefObject<HTMLDialogElement | null>;
  historyState: HistoryLoadState;
  onClose: () => void;
  onClosed: () => void;
  selectedDay: SelectedDay | null;
  sessions: TrainingSessionHistorySummary[];
}) {
  const daySessions = selectedDay
    ? sessions.filter((session) => session.estado === TRAINING_SESSION_STATUS.completed && getLocalDateKey(session.horaInicio) === selectedDay.key)
    : [];
  const date = selectedDay ? parseLocalDateKey(selectedDay.key) : null;

  return (
    <dialog
      aria-labelledby="activity-day-title"
      aria-modal="true"
      className="mt-auto max-h-[85dvh] w-full max-w-full rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-lg sm:rounded-container"
      onClick={(event) => { if (event.target === event.currentTarget) onClose(); }}
      onClose={onClosed}
      ref={dialogRef}
    >
      <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
      <div className="max-h-[calc(85dvh-0.75rem)] overflow-y-auto px-4 pb-[calc(1.5rem+env(safe-area-inset-bottom))] pt-3 sm:px-6 sm:pb-6 sm:pt-5">
        <header className="flex items-start gap-3">
          <div className="min-w-0 flex-1">
            <h3 className="font-brand text-xl font-bold uppercase leading-7 text-text-primary" id="activity-day-title">{date ? formatAccessibleDate(date).toLocaleUpperCase("es-AR") : "Actividad"}</h3>
            {selectedDay ? <p className="mt-1 text-sm text-text-secondary">{selectedDay.sessionCount} {selectedDay.sessionCount === 1 ? "entrenamiento" : "entrenamientos"}</p> : null}
          </div>
          <button aria-label="Cerrar detalle del día" className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary hover:bg-surface hover:text-text-primary focus-visible:outline-primary" onClick={onClose} type="button"><X aria-hidden="true" size={20} /></button>
        </header>

        {historyState === "loading" ? (
          <p className="mt-6 text-sm text-text-secondary" role="status">Cargando entrenamientos…</p>
        ) : historyState === "error" ? (
          <p className="mt-6 text-sm text-text-secondary" role="alert">No pudimos cargar los entrenamientos de este día.</p>
        ) : daySessions.length === 0 ? (
          <p className="mt-6 text-sm text-text-secondary">No encontramos el detalle de estos entrenamientos.</p>
        ) : (
          <ul className="mt-5 divide-y divide-border/50 border-y border-border/50">
            {daySessions.map((session) => {
              const duration = formatDuration(session.horaInicio, session.horaFin);
              const metadata = [formatExerciseCount(session.cantidadEjercicios), formatSetCount(session.cantidadSeriesCompletadas), duration].filter((value): value is string => Boolean(value));
              return (
                <li key={session.id}>
                  <Link className="group flex min-h-20 items-center gap-3 py-3 focus-visible:outline-primary" href={`/history/${session.id}`}>
                    <time className="w-12 shrink-0 self-start pt-0.5 font-brand text-lg font-bold tabular-nums" dateTime={session.horaInicio}>{formatHistoryTime(session.horaInicio) ?? "—"}</time>
                    <span className="min-w-0 flex-1">
                      <span className="block break-words text-sm leading-6 text-text-secondary">{metadata.join(" · ")}</span>
                      <span className="mt-1 block text-sm font-semibold text-primary">Ver entrenamiento</span>
                    </span>
                    <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary group-hover:text-primary" size={19} />
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </dialog>
  );
}

function getActivityClasses(count: number, future: boolean): string {
  if (future) return "border-border/25 bg-surface/20 text-text-secondary/35";
  if (count > 0) return "border-primary/65 bg-primary-strong/85 text-text-primary";
  return "border-border/45 bg-surface-elevated/40 text-text-secondary";
}

function getDayAriaLabel(date: Date, count: number, future: boolean): string {
  const dateLabel = formatAccessibleDate(date);
  if (future) return `${dateLabel}: día futuro`;
  if (count === 0) return `${dateLabel}: sin entrenamientos`;
  return `${dateLabel}: ${count} ${count === 1 ? "entrenamiento completado" : "entrenamientos completados"}`;
}
