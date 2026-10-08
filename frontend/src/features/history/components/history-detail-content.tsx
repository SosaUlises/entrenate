"use client";

import { ArrowLeft, ChevronRight } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import type { TrainingSession, TrainingSessionExercise } from "@/features/training/types/training.types";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getTrainingSessionDetailAction } from "../actions/get-training-session-detail.action";
import {
  formatDecimal,
  formatDuration,
  formatExerciseCount,
  formatHistoryDetailDate,
  formatHistoryTime,
  formatSetCount,
} from "../history-formatters";
import { TRAINING_SESSION_STATUS } from "../types/history.types";

type DetailState =
  | { status: "loading" }
  | { status: "not-found" }
  | { status: "error" }
  | { status: "ready"; session: TrainingSession };

export function HistoryDetailContent({ id }: { id: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<DetailState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

  useEffect(() => {
    let active = true;

    void getTrainingSessionDetailAction(id).then((result) => {
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
  }, [id, invalidateSession, requestKey, router]);

  const session = state.status === "ready" ? state.session : null;

  return (
    <section aria-labelledby="history-detail-title" className="mx-auto w-full max-w-xl">
      <div className="flex min-w-0 items-center gap-1">
        <Link aria-label="Volver al historial" className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary" href="/history">
          <ArrowLeft aria-hidden="true" size={20} />
        </Link>
        <h1 className="font-brand text-2xl font-bold text-text-primary" id="history-detail-title">Entrenamiento</h1>
      </div>

      {state.status === "loading" ? (
        <DetailSkeleton />
      ) : state.status === "not-found" ? (
        <div className="mt-8">
          <p className="text-sm text-text-primary">No encontramos este entrenamiento.</p>
          <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href="/history">Volver al historial</Link>
        </div>
      ) : state.status === "error" ? (
        <div className="mt-8" role="alert">
          <p className="text-sm text-text-primary">No pudimos cargar este entrenamiento.</p>
          <Button className="mt-5" onClick={() => { setState({ status: "loading" }); setRequestKey((key) => key + 1); }} variant="secondary">Reintentar</Button>
        </div>
      ) : session ? (
        <SessionDetail session={session} />
      ) : null}
    </section>
  );
}

function SessionDetail({ session }: { session: TrainingSession }) {
  const exercises = [...session.ejercicios].sort((first, second) => first.orden - second.orden);
  const registeredSetCount = exercises.reduce((total, exercise) => total + exercise.series.length, 0);
  const date = formatHistoryDetailDate(session.horaInicio);
  const time = formatHistoryTime(session.horaInicio);
  const duration = formatDuration(session.horaInicio, session.horaFin);
  const cancelled = session.estado === TRAINING_SESSION_STATUS.cancelled;
  const summary = [duration, formatExerciseCount(exercises.length), formatSetCount(registeredSetCount)].filter((value): value is string => Boolean(value));

  return (
    <>
      <div className="mt-3 pl-12">
        {date || time ? <p className="text-[0.9375rem] font-medium leading-6 text-text-secondary">{[date, time].filter(Boolean).join(" · ")}</p> : null}
        <p className={`mt-4 text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.11em] ${cancelled ? "text-warning/85" : "text-success"}`}>
          {cancelled ? "Cancelado" : "Completado"}
        </p>
        <p className="mt-1.5 text-[0.9375rem] font-medium leading-6 text-text-primary/75">{summary.join(" · ")}</p>
      </div>

      <div className="mt-9 border-t border-border/60">
        {exercises.map((exercise) => <ExerciseResult exercise={exercise} key={exercise.id} />)}
      </div>
    </>
  );
}

function ExerciseResult({ exercise }: { exercise: TrainingSessionExercise }) {
  const sets = [...exercise.series].sort((first, second) => first.numeroSerie - second.numeroSerie);
  const prescription = [
    `${exercise.seriesObjetivo}×${exercise.repeticionesMinimasObjetivo}–${exercise.repeticionesMaximasObjetivo}`,
    `RIR ${exercise.rirObjetivoMinimo}–${exercise.rirObjetivoMaximo}`,
    `${exercise.descansoObjetivoSegundos} s`,
  ].join(" · ");

  return (
    <section aria-labelledby={`history-exercise-${exercise.id}`} className="border-b border-border/60 py-6">
      <h2 className="break-words font-brand text-xl font-bold leading-7 text-text-primary" id={`history-exercise-${exercise.id}`}>{exercise.nombre}</h2>
      <p className="mt-1.5 text-[0.9375rem] font-medium leading-6 text-text-secondary">{prescription}</p>
      {exercise.notas?.trim() ? <p className="mt-2 text-[0.8125rem] leading-5 text-text-secondary/85">{exercise.notas}</p> : null}
      <Link
        className="mt-2 inline-flex min-h-11 items-center gap-1 text-sm font-semibold text-primary hover:text-text-primary focus-visible:outline-primary"
        href={`/exercises/${exercise.ejercicioId}/progress`}
      >
        Ver progreso
        <ChevronRight aria-hidden="true" size={17} />
      </Link>

      {sets.length === 0 ? (
        <p className="mt-5 text-sm text-text-secondary">Sin series registradas</p>
      ) : (
        <div aria-label={`Series registradas de ${exercise.nombre}`} className="mt-5 w-full min-w-0" role="table">
          <div className="grid grid-cols-[minmax(2.75rem,0.75fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(2.5rem,0.7fr)] gap-x-2 border-b border-border/50 pb-2.5 text-left text-xs font-semibold uppercase tracking-[0.07em] text-text-secondary" role="row">
            <span role="columnheader">Set</span>
            <span role="columnheader">Kg</span>
            <span role="columnheader">Reps</span>
            <span role="columnheader">RIR</span>
          </div>
          <div className="divide-y divide-border/35" role="rowgroup">
            {sets.map((set) => {
              const extra = set.numeroSerie > exercise.seriesObjetivo;
              return (
                <div className="grid min-h-12 grid-cols-[minmax(2.75rem,0.75fr)_minmax(0,1fr)_minmax(0,1fr)_minmax(2.5rem,0.7fr)] items-center gap-x-2 py-2.5 font-brand text-lg font-bold leading-6 tabular-nums text-text-primary" key={set.id} role="row">
                  <span className="min-w-0" role="cell">
                    {String(set.numeroSerie).padStart(2, "0")}
                    {extra ? <span className="block font-sans text-[0.6875rem] font-semibold uppercase tracking-[0.07em] text-info">Extra</span> : null}
                  </span>
                  <span className="min-w-0 truncate" role="cell">{formatDecimal(set.peso)}</span>
                  <span className="min-w-0 truncate" role="cell">{set.repeticiones}</span>
                  <span className="min-w-0 truncate" role="cell">{set.rir ?? "—"}</span>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </section>
  );
}

function DetailSkeleton() {
  return (
    <div aria-label="Cargando entrenamiento" className="mt-6 animate-pulse pl-12" role="status">
      <div className="h-3 w-36 rounded bg-surface-elevated" />
      <div className="mt-5 h-3 w-24 rounded bg-surface-elevated" />
      <div className="mt-3 h-3 w-full max-w-72 rounded bg-surface-elevated" />
      <div className="mt-9 border-t border-border/50 pt-6">
        <div className="h-5 w-52 rounded bg-surface-elevated" />
        <div className="mt-3 h-3 w-44 rounded bg-surface-elevated" />
        <div className="mt-6 h-28 w-full rounded-control-sm bg-surface/60" />
      </div>
    </div>
  );
}
