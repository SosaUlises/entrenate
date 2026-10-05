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

type HistoryState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; sessions: TrainingSessionHistorySummary[] };

type HistoryGroup = {
  key: string;
  label: string;
  sessions: TrainingSessionHistorySummary[];
};

export function HistoryContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<HistoryState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

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

  const groups = state.status === "ready" ? groupSessions(state.sessions) : [];

  return (
    <section aria-labelledby="history-title" className="mx-auto w-full max-w-xl">
      <header>
        <h1 className="font-brand text-2xl font-bold text-text-primary" id="history-title">Historial</h1>
        <p className="mt-1.5 text-sm leading-6 text-text-secondary">Tus entrenamientos registrados.</p>
      </header>

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
        <div className="mt-10 max-w-sm">
          <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary">Todavía no registraste entrenamientos.</h2>
          <p className="mt-3 text-sm leading-6 text-text-secondary">Cuando completes una sesión, aparecerá acá.</p>
          <Link className="mt-6 inline-flex min-h-12 items-center rounded-control-sm bg-primary-strong px-4 text-sm font-semibold text-text-primary transition-colors hover:bg-primary focus-visible:outline-primary" href="/routines">
            Elegir entrenamiento
          </Link>
        </div>
      ) : (
        <div className="mt-8 space-y-9">
          {groups.map((group) => (
            <section aria-labelledby={`history-date-${group.key}`} key={group.key}>
              <h2 className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.1em] text-text-secondary" id={`history-date-${group.key}`}>
                {group.label}
              </h2>
              <ul className="mt-2.5 divide-y divide-border/50 border-y border-border/50">
                {group.sessions.map((session) => <HistoryRow key={session.id} session={session} />)}
              </ul>
            </section>
          ))}
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
      <Link className="group flex min-h-[6.25rem] min-w-0 items-center gap-3.5 rounded-control-sm px-1 py-5 transition-colors hover:bg-surface/35 active:bg-surface/60 focus-visible:outline-2 focus-visible:outline-primary" href={`/history/${session.id}`}>
        <time className="w-13 shrink-0 self-start pt-0.5 font-brand text-lg font-bold leading-6 tabular-nums text-text-primary" dateTime={session.horaInicio}>
          {time ?? "—"}
        </time>
        <div className="min-w-0 flex-1">
          <h3 className="font-brand text-[1.0625rem] font-bold leading-6 text-text-primary">
            {cancelled ? "Entrenamiento cancelado" : "Entrenamiento"}
          </h3>
          <p className="mt-1.5 break-words text-[0.9375rem] leading-6 text-text-secondary">{metadata.join(" · ")}</p>
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
    <div aria-label="Cargando historial" className="mt-8 animate-pulse" role="status">
      <div className="h-3 w-24 rounded bg-surface-elevated" />
      <div className="mt-3 divide-y divide-border/40 border-y border-border/40">
        {[0, 1, 2].map((item) => (
          <div className="flex min-h-[6.25rem] items-center gap-3.5 py-5" key={item}>
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
