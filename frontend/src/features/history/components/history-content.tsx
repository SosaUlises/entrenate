"use client";

import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getTrainingHistoryAction } from "../actions/get-training-history.action";
import {
  formatDuration,
  formatExerciseCount,
  formatHistoryGroupDate,
  formatHistoryTime,
  formatSetCount,
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
  sessions: TrainingSessionHistorySummary[];
};

const initialVisibleSessionCount = 5;
const sessionCountIncrement = 5;

export function HistoryContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<HistoryState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [visibleSessionCount, setVisibleSessionCount] = useState(initialVisibleSessionCount);

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
        <h1 className="font-brand text-[1.625rem] font-bold leading-8 text-text-primary" id="history-title">Historial</h1>
        <p className="mt-1 text-sm leading-5 text-text-secondary">Tus entrenamientos registrados.</p>
      </header>

      <TrainingActivityCalendar
        historyState={historyLoadState}
        sessions={state.status === "ready" ? state.sessions : []}
      />

      <h2 className="mt-8 text-xs font-semibold uppercase tracking-[0.14em] text-text-secondary sm:mt-10">
        Últimos entrenamientos
      </h2>

      {state.status === "loading" ? (
        <HistorySkeleton />
      ) : state.status === "error" ? (
        <div className="mt-9" role="alert">
          <p className="text-sm text-text-primary">No pudimos cargar tu historial.</p>
          <Button className="mt-5" onClick={() => { setState({ status: "loading" }); setRequestKey((key) => key + 1); }} variant="secondary">
            Reintentar
          </Button>
        </div>
      ) : state.sessions.length === 0 ? (
        <div className="mt-8 max-w-sm">
          <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary">Todavía no registraste entrenamientos.</h2>
          <p className="mt-3 text-sm leading-6 text-text-secondary">Cuando completes una sesión, aparecerá acá.</p>
          <Link className="mt-6 inline-flex min-h-12 items-center rounded-control-sm bg-primary-strong px-4 text-sm font-semibold text-text-primary transition-colors hover:bg-primary focus-visible:outline-primary" href="/routines">
            Elegir entrenamiento
          </Link>
        </div>
      ) : (
        <div className="mt-4 min-w-0 max-w-full">
          <div className="min-w-0 max-w-full space-y-7 sm:space-y-8">
            {groups.map((group) => (
              <section aria-labelledby={`history-date-${group.key}`} className="min-w-0 max-w-full" key={group.key}>
                <h2 className="text-xs font-semibold uppercase leading-5 tracking-[0.1em] text-text-secondary" id={`history-date-${group.key}`}>
                  {group.label}
                </h2>
                <ul className="mt-2 min-w-0 max-w-full divide-y divide-border/50 border-y border-border/50">
                  {group.sessions.map((session) => <HistoryRow key={session.id} session={session} />)}
                </ul>
              </section>
            ))}
          </div>
          {hasMoreSessions ? (
            <Button
              className="mt-5 min-h-10 rounded-full px-4"
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
  const metadata = [
    formatExerciseCount(session.cantidadEjercicios),
    formatSetCount(session.cantidadSeriesCompletadas),
    duration,
  ].filter((value): value is string => Boolean(value));

  return (
    <li>
      <Link className="group flex min-h-[5.25rem] min-w-0 items-center gap-3 rounded-control-sm px-1 py-4 transition-colors hover:bg-surface/35 active:bg-surface/60 focus-visible:outline-2 focus-visible:outline-primary sm:min-h-[5.75rem] sm:gap-3.5 sm:py-5" href={`/history/${session.id}`}>
        <time className="w-12 shrink-0 self-start pt-0.5 font-brand text-[1.0625rem] font-bold leading-6 tabular-nums text-text-primary sm:w-13 sm:text-lg" dateTime={session.horaInicio}>
          {time ?? "—"}
        </time>
        <div className="min-w-0 flex-1">
          <h3 className="font-brand text-[1.0625rem] font-bold leading-6 text-text-primary">
            {cancelled ? "Entrenamiento cancelado" : "Entrenamiento"}
          </h3>
          <p className="mt-1 break-words text-sm leading-5 text-text-secondary sm:mt-1.5 sm:text-[0.9375rem] sm:leading-6">{metadata.join(" · ")}</p>
        </div>
        <span aria-hidden="true" className="flex size-11 shrink-0 items-center justify-center text-text-secondary/70 transition-colors group-hover:text-primary group-active:text-primary">
          <ChevronRight size={21} strokeWidth={1.8} />
        </span>
      </Link>
    </li>
  );
}

function HistorySkeleton() {
  return (
    <div aria-label="Cargando historial" className="mt-5 animate-pulse" role="status">
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
      groups.set(key, { key, label: formatHistoryGroupDate(session.horaInicio), sessions: [session] });
    }
  }

  return [...groups.values()];
}
