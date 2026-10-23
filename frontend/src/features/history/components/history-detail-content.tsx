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
  formatHistoryDetailDate,
  formatHistoryTime,
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
    <section aria-label="Detalle del historial" className="mx-auto w-full max-w-xl">
      <Link className="inline-flex min-h-11 items-center gap-2 rounded-control-sm pr-3 text-sm font-semibold text-text-secondary transition-colors hover:text-text-primary focus-visible:outline-focus" href="/history">
        <ArrowLeft aria-hidden="true" size={20} />
        Historial
      </Link>

      {state.status === "loading" ? (
        <DetailSkeleton />
      ) : state.status === "not-found" ? (
        <div className="mt-6">
          <h1 className="font-brand text-2xl font-bold text-text-primary">Entrenamiento no encontrado</h1>
          <p className="mt-3 text-sm text-text-secondary">No encontramos este entrenamiento.</p>
          <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-thermal-accent transition-colors hover:text-text-primary focus-visible:outline-focus" href="/history">Volver al historial</Link>
        </div>
      ) : state.status === "error" ? (
        <div className="mt-6" role="alert">
          <h1 className="font-brand text-2xl font-bold text-text-primary">No pudimos cargar este entrenamiento</h1>
          <p className="mt-3 text-sm text-text-secondary">Revisá tu conexión e intentá nuevamente.</p>
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
  const registeredSetCount = exercises.reduce(
    (total, exercise) => total + exercise.series.filter((set) => set.completada).length,
    0,
  );
  const date = formatHistoryDetailDate(session.horaInicio);
  const time = formatHistoryTime(session.horaInicio);
  const duration = formatDuration(session.horaInicio, session.horaFin);
  const cancelled = session.estado === TRAINING_SESSION_STATUS.cancelled;
  const completed = session.estado === TRAINING_SESSION_STATUS.completed;

  return (
    <>
      <header className="mt-5">
        <h1 className="font-brand text-3xl font-bold leading-tight text-text-primary">{date ?? "Fecha no disponible"}</h1>
        <p className="mt-2 text-[0.9375rem] font-semibold leading-6 text-steel">
          {[time, duration].filter(Boolean).join(" · ")}
        </p>
        <p className={`mt-3 text-[0.8125rem] font-semibold leading-5 ${cancelled ? "text-warning/85" : "text-success"}`}>
          {cancelled ? "Cancelada" : completed ? "Completada" : "En curso"}
        </p>
      </header>

      <dl className="mt-7 grid grid-cols-3 divide-x divide-border/70 border-y border-border/70 py-5">
        <SummaryValue label="Ejercicios" value={String(exercises.length)} />
        <SummaryValue label="Series" value={String(registeredSetCount)} />
        <SummaryValue label="Duración" value={duration ?? "—"} />
      </dl>

      <div className="mt-10 border-t border-border/70">
        {exercises.map((exercise) => (
          <ExerciseResult
            canShowProgress={completed && exercise.series.some((set) => set.completada)}
            exercise={exercise}
            key={exercise.id}
          />
        ))}
      </div>
    </>
  );
}

function SummaryValue({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0 px-3 first:pl-0 last:pr-0">
      <dt className="text-[0.8125rem] font-semibold leading-5 text-steel">{label}</dt>
      <dd className="mt-1.5 break-words font-brand text-xl font-bold leading-6 tabular-nums text-text-primary">{value}</dd>
    </div>
  );
}

function ExerciseResult({
  canShowProgress,
  exercise,
}: {
  canShowProgress: boolean;
  exercise: TrainingSessionExercise;
}) {
  const sets = [...exercise.series].sort((first, second) => first.numeroSerie - second.numeroSerie);
  const prescription = [
    `${exercise.seriesObjetivo}×${exercise.repeticionesMinimasObjetivo}–${exercise.repeticionesMaximasObjetivo}`,
    `RIR ${exercise.rirObjetivoMinimo}–${exercise.rirObjetivoMaximo}`,
    `${exercise.descansoObjetivoSegundos} s`,
  ].join(" · ");

  return (
    <section aria-labelledby={`history-exercise-${exercise.id}`} className="border-b border-border/70 py-8">
      <div className="flex min-w-0 items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <h2 className="break-words font-brand text-xl font-bold leading-7 text-text-primary" id={`history-exercise-${exercise.id}`}>{exercise.nombre}</h2>
          <p className="mt-2 text-[0.8125rem] font-semibold leading-5 text-steel">Objetivo · {prescription}</p>
        </div>
        {canShowProgress ? (
          <Link
            className="inline-flex min-h-11 shrink-0 items-center gap-1 rounded-control-sm px-2 text-sm font-semibold text-text-muted transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary active:bg-surface-active focus-visible:text-text-primary focus-visible:outline-focus motion-reduce:transition-none"
            href={`/exercises/${exercise.ejercicioId}/progress`}
          >
            Ver progreso
            <ChevronRight aria-hidden="true" size={17} />
          </Link>
        ) : null}
      </div>

      {exercise.notas?.trim() ? <p className="mt-2 text-[0.8125rem] leading-5 text-text-secondary/85">{exercise.notas}</p> : null}

      {sets.length === 0 ? (
        <p className="mt-5 text-sm text-text-secondary">Sin series registradas</p>
      ) : (
        <div className="mt-5 w-full min-w-0 overflow-x-auto">
          <table className="w-full table-fixed border-collapse text-left">
            <caption className="sr-only">Series registradas de {exercise.nombre}</caption>
            <thead className="border-b border-border/60 text-[0.8125rem] font-semibold uppercase tracking-[0.06em] text-steel">
              <tr>
                <th className="w-[24%] pb-2.5" scope="col">Set</th>
                <th className="w-[26%] pb-2.5" scope="col">Kg</th>
                <th className="w-[28%] pb-2.5" scope="col">Reps</th>
                <th className="w-[22%] pb-2.5" scope="col">RIR</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/35">
              {sets.map((set) => {
                const extra = set.numeroSerie > exercise.seriesObjetivo;
                return (
                  <tr className="h-12 font-brand text-lg font-bold leading-6 tabular-nums text-text-primary" key={set.id}>
                    <td className="min-w-0 py-2.5">
                      {String(set.numeroSerie).padStart(2, "0")}
                      {extra ? <span className="block font-sans text-[0.8125rem] font-semibold leading-4 text-steel">Extra</span> : null}
                    </td>
                    <td className="min-w-0 truncate py-2.5">
                      {set.peso > 0 ? formatDecimal(set.peso) : (
                        <>
                          <span aria-hidden="true">—</span>
                          <span className="sr-only">Sin carga registrada</span>
                        </>
                      )}
                    </td>
                    <td className="min-w-0 truncate py-2.5">{set.repeticiones}</td>
                    <td className="min-w-0 truncate py-2.5">
                      {set.rir ?? (
                        <>
                          <span aria-hidden="true">—</span>
                          <span className="sr-only">Sin RIR registrado</span>
                        </>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function DetailSkeleton() {
  return (
    <div aria-label="Cargando entrenamiento" className="mt-5 animate-pulse" role="status">
      <div className="h-8 w-52 rounded bg-surface-elevated" />
      <div className="mt-3 h-4 w-36 rounded bg-surface-elevated" />
      <div className="mt-3 h-4 w-20 rounded bg-surface-elevated" />
      <div className="mt-7 grid grid-cols-3 gap-5 border-y border-border/50 py-4">
        {[0, 1, 2].map((item) => <div className="h-11 rounded bg-surface-elevated" key={item} />)}
      </div>
      <div className="mt-8 border-t border-border/50 pt-6">
        <div className="h-6 w-52 rounded bg-surface-elevated" />
        <div className="mt-3 h-4 w-44 rounded bg-surface-elevated" />
        <div className="mt-6 h-36 w-full rounded-control-sm bg-surface/60" />
      </div>
    </div>
  );
}
