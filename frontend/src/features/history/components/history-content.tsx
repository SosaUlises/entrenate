"use client";

import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getTrainingHistoryAction } from "../actions/get-training-history.action";
import {
  formatCompactSetCount,
  formatDuration,
  formatExerciseCount,
  formatHistoryAccessibleDate,
  formatHistoryGroupDate,
  formatHistoryTime,
  getLocalDateKey,
} from "../history-formatters";
import { TRAINING_SESSION_STATUS, type TrainingSessionHistorySummary } from "../types/history.types";
import { TrainingActivityCalendar } from "./training-activity-calendar";

type HistoryState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; sessions: TrainingSessionHistorySummary[] };

type HistoryGroup = {
  key: string;
  label: string;
  accessibleLabel: string;
  sessions: TrainingSessionHistorySummary[];
};

type HistoryView = "sessions" | "activity";

const initialVisibleSessionCount = 5;
const sessionCountIncrement = 5;

export function HistoryContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<HistoryState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [visibleSessionCount, setVisibleSessionCount] = useState(initialVisibleSessionCount);
  const [view, setView] = useState<HistoryView>("activity");

  useEffect(() => {
    let active = true;

    void getTrainingHistoryAction().then((result) => {
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      setState(result);
    }, () => {
      if (active) setState({ status: "error" });
    });

    return () => { active = false; };
  }, [invalidateSession, requestKey, router]);

  const visibleSessions = state.status === "ready"
    ? state.sessions.slice(0, visibleSessionCount)
    : [];
  const groups = groupSessions(visibleSessions);
  const hasMoreSessions = state.status === "ready" && visibleSessionCount < state.sessions.length;
  const historyLoadState = state.status === "ready" ? "ready" : state.status;

  return (
    <section aria-labelledby="history-title" className="mx-auto min-w-0 w-full max-w-xl overflow-x-clip">
      <header className="pb-1">
        <h1 className="font-brand text-3xl font-bold leading-tight text-text-primary" id="history-title">Historial</h1>
        <p className="mt-2 text-base leading-6 text-text-secondary">Tus entrenamientos registrados.</p>
      </header>

      <div aria-label="Vista de historial" className="mt-6 inline-grid grid-cols-2 gap-2 border-b border-border/70" role="group">
        {(["activity", "sessions"] as const).map((item) => {
          const selected = view === item;
          const label = item === "sessions" ? "Entrenamientos" : "Actividad";
          return (
            <button
              aria-pressed={selected}
              className={`relative min-h-12 px-3 text-[0.8125rem] font-semibold transition-colors duration-150 focus-visible:outline-focus motion-reduce:transition-none ${selected ? "text-text-primary after:absolute after:right-3 after:bottom-0 after:left-3 after:h-0.5 after:rounded-full after:bg-thermal-accent" : "text-text-muted hover:text-text-secondary"}`}
              key={item}
              onClick={() => setView(item)}
              type="button"
            >
              {label}
            </button>
          );
        })}
      </div>

      {view === "activity" ? (
        <TrainingActivityCalendar
          historyState={historyLoadState}
          sessions={state.status === "ready" ? state.sessions : []}
        />
      ) : state.status === "loading" ? (
        <HistorySkeleton />
      ) : state.status === "error" ? (
        <div className="mt-8" role="alert">
          <p className="text-sm text-text-primary">No pudimos cargar tu historial.</p>
          <Button className="mt-5" onClick={() => { setState({ status: "loading" }); setRequestKey((key) => key + 1); }} variant="secondary">
            Reintentar
          </Button>
        </div>
      ) : state.sessions.length === 0 ? (
        <div className="mt-8 max-w-sm">
          <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary">Todavía no registraste entrenamientos.</h2>
          <p className="mt-3 text-sm leading-6 text-text-secondary">Las sesiones completadas o canceladas van a aparecer acá.</p>
          <Link className="mt-5 inline-flex min-h-11 items-center text-sm font-semibold text-thermal-accent transition-colors hover:text-text-primary focus-visible:outline-focus" href="/routines">
            Ver rutinas
          </Link>
        </div>
      ) : (
        <div className="mt-6 min-w-0 max-w-full">
          <div className="min-w-0 max-w-full space-y-7 sm:space-y-8">
            {groups.map((group) => (
              <section aria-labelledby={`history-date-${group.key}`} className="min-w-0 max-w-full" key={group.key}>
                <h2 className="text-[0.8125rem] font-semibold leading-5 tracking-[0.08em] text-steel" id={`history-date-${group.key}`}>
                  <time aria-label={group.accessibleLabel} dateTime={group.key}>{group.label}</time>
                </h2>
                <ul className="mt-2 min-w-0 max-w-full divide-y divide-border/50 border-y border-border/50">
                  {group.sessions.map((session) => <HistoryRow key={session.id} session={session} />)}
                </ul>
              </section>
            ))}
          </div>
          {hasMoreSessions ? (
            <Button
              className="mt-6"
              onClick={() => setVisibleSessionCount((current) => current + sessionCountIncrement)}
              variant="secondary"
            >
              Ver más
            </Button>
          ) : null}
        </div>
      )}
    </section>
  );
}

function HistoryRow({ session }: { session: TrainingSessionHistorySummary }) {
  const time = formatHistoryTime(session.horaInicio);
  const duration = formatDuration(session.horaInicio, session.horaFin);
  const cancelled = session.estado === TRAINING_SESSION_STATUS.cancelled;
  const registered = [
    formatExerciseCount(session.cantidadEjercicios),
    formatCompactSetCount(session.cantidadSeriesCompletadas),
  ].join(" · ");
  const accessibleLabel = [
    formatHistoryAccessibleDate(session.horaInicio),
    time ? `a las ${time}` : null,
    registered,
    duration,
    cancelled ? "Cancelada" : "Completada",
  ].filter(Boolean).join(". ");

  return (
    <li>
      <Link aria-label={accessibleLabel} className="group flex min-h-[5.5rem] min-w-0 items-center gap-4 rounded-control-sm px-1 py-4 transition-colors hover:bg-surface-interactive/45 active:bg-surface-active/55 focus-visible:outline-2 focus-visible:outline-focus sm:min-h-[5.75rem] sm:py-5" href={`/history/${session.id}`}>
        <time className="w-12 shrink-0 self-start pt-0.5 text-[0.9375rem] font-semibold leading-6 tabular-nums text-text-muted sm:w-13" dateTime={session.horaInicio}>
          {time ?? "—"}
        </time>
        <span aria-hidden="true" className="min-w-0 flex-1">
          <span className="block break-words font-brand text-[1.0625rem] font-bold leading-6 text-text-primary">{registered}</span>
          {duration || cancelled ? (
            <span className="mt-1.5 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[0.8125rem] leading-5 text-steel">
              {duration ? <span>{duration}</span> : null}
              {cancelled ? <span className="font-semibold text-warning/85">Cancelada</span> : null}
            </span>
          ) : null}
        </span>
        <span aria-hidden="true" className="flex size-11 shrink-0 items-center justify-center text-text-muted transition-colors group-hover:text-thermal-accent group-active:text-thermal-accent">
          <ChevronRight size={21} strokeWidth={1.8} />
        </span>
      </Link>
    </li>
  );
}

function HistorySkeleton() {
  return (
    <div aria-label="Cargando historial" className="mt-6 animate-pulse" role="status">
      <div className="h-3 w-24 rounded bg-surface-elevated" />
      <div className="mt-3 divide-y divide-border/40 border-y border-border/40">
        {[0, 1, 2].map((item) => (
          <div className="flex min-h-[5.25rem] items-center gap-3 py-4 sm:min-h-[5.75rem] sm:gap-3.5 sm:py-5" key={item}>
            <div className="h-5 w-12 rounded bg-surface-elevated" />
            <div className="flex-1 space-y-3">
              <div className="h-4 w-40 rounded bg-surface-elevated" />
              <div className="h-3 w-full max-w-64 rounded bg-surface-elevated" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function groupSessions(sessions: TrainingSessionHistorySummary[]): HistoryGroup[] {
  const groups = new Map<string, HistoryGroup>();

  for (const session of sessions) {
    const key = getLocalDateKey(session.horaInicio);
    const existing = groups.get(key);
    if (existing) {
      existing.sessions.push(session);
    } else {
      groups.set(key, {
        key,
        label: formatHistoryGroupDate(session.horaInicio),
        accessibleLabel: formatHistoryAccessibleDate(session.horaInicio) ?? "Fecha no disponible",
        sessions: [session],
      });
    }
  }

  return [...groups.values()];
}
