"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ChevronDown, SlidersHorizontal } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getActiveTrainingSessionAction, startTrainingSessionAction } from "@/features/training/actions/training.actions";
import { ActiveSessionRecoveryDialog } from "@/features/training/components/active-session-recovery-dialog";
import type { TrainingSession } from "@/features/training/types/training.types";
import { getRoutineDetailAction, type GetRoutineDetailActionResult } from "../actions/get-routine-detail.action";
import { RoutineExerciseTarget } from "./routine-exercise-target";

type DetailState = { status: "loading" } | GetRoutineDetailActionResult;

export function RoutineDetailContent({ id }: { id: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<DetailState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [expandedDayId, setExpandedDayId] = useState<string | null>(null);
  const [startingDayId, setStartingDayId] = useState<string | null>(null);
  const [startError, setStartError] = useState<{ dayId: string; kind: "verification" | "start" } | null>(null);
  const [recoverySession, setRecoverySession] = useState<TrainingSession | null>(null);
  const [startNotice, setStartNotice] = useState<string | null>(null);

  function handleUnauthenticated() {
    invalidateSession();
    router.replace("/login");
  }

  async function recoverActiveSession(dayId: string): Promise<"none" | "blocked" | "unauthenticated" | "error"> {
    const activeResult = await getActiveTrainingSessionAction();
    if (activeResult.status === "success") {
      setRecoverySession(activeResult.data);
      return "blocked";
    }
    if (activeResult.status === "unauthenticated") {
      handleUnauthenticated();
      return "unauthenticated";
    }
    if (activeResult.status === "not-found") return "none";

    setStartError({ dayId, kind: "verification" });
    return "error";
  }

  async function handleStart(dayId: string) {
    if (startingDayId) return;
    setStartingDayId(dayId);
    setStartError(null);
    setStartNotice(null);
    try {
      const activeState = await recoverActiveSession(dayId);
      if (activeState !== "none") return;

      const result = await startTrainingSessionAction(dayId);
      if (result.status === "success") {
        router.push("/training");
        return;
      }
      if (result.status === "unauthenticated") {
        handleUnauthenticated();
        return;
      }
      if (result.status === "conflict") {
        const recoveryState = await recoverActiveSession(dayId);
        if (recoveryState === "none") {
          setStartError({ dayId, kind: "start" });
        }
        return;
      }
      setStartError({ dayId, kind: "start" });
    } catch {
      setStartError({ dayId, kind: "verification" });
    } finally {
      setStartingDayId(null);
    }
  }

  useEffect(() => {
    let active = true;

    void getRoutineDetailAction(id).then((result) => {
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      if (result.status === "ready") {
        const firstDay = [...result.routine.dias].sort((first, second) => first.orden - second.orden)[0];
        setExpandedDayId(firstDay?.id ?? null);
      }
      setState(result);
    }, () => {
      if (active) setState({ status: "error" });
    });

    return () => { active = false; };
  }, [id, invalidateSession, requestKey, router]);

  const routine = state.status === "ready" ? state.routine : null;
  const exerciseCount = routine?.dias.reduce((total, day) => total + day.ejercicios.length, 0) ?? 0;

  return (
    <section aria-labelledby="routine-detail-title" className="mx-auto w-full max-w-xl">
      <div className="flex min-w-0 items-center gap-1">
        <Link
          aria-label="Volver a mis rutinas"
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
          href="/routines"
        >
          <ArrowLeft aria-hidden="true" size={20} />
        </Link>
        <h1 className="min-w-0 truncate font-brand text-2xl font-bold text-text-primary" id="routine-detail-title" title={routine?.nombre}>
          {routine?.nombre ?? "Rutina"}
        </h1>
        {routine ? (
          <Link className="ml-auto inline-flex min-h-11 shrink-0 items-center rounded-control-sm px-3 text-sm font-semibold text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus" href={`/routines/${routine.id}/edit`}>
            Editar
          </Link>
        ) : null}
      </div>
      {routine ? (
        <p className="mt-1 pl-12 text-sm text-text-secondary">
          {routine.dias.length} {routine.dias.length === 1 ? "día" : "días"} · {exerciseCount} {exerciseCount === 1 ? "ejercicio" : "ejercicios"}
        </p>
      ) : null}
      {startNotice ? <p className="mt-4 text-sm text-success" role="status">{startNotice}</p> : null}

      {state.status === "loading" || state.status === "unauthenticated" ? (
        <RoutineDetailSkeleton />
      ) : state.status === "not-found" ? (
        <div className="mt-8">
          <p className="text-sm text-text-primary">No encontramos esta rutina.</p>
          <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-text-primary focus-visible:outline-2 focus-visible:outline-focus" href="/routines">
            Volver a mis rutinas
          </Link>
        </div>
      ) : state.status === "error" ? (
        <div className="mt-8">
          <p className="text-sm text-text-primary">No pudimos cargar la rutina.</p>
          <Button
            className="mt-5"
            onClick={() => {
              setState({ status: "loading" });
              setRequestKey((key) => key + 1);
            }}
            variant="secondary"
          >
            Reintentar
          </Button>
        </div>
      ) : (
        <>
          {state.routine.descripcion?.trim() ? (
            <p className="mt-2 pl-12 text-sm leading-6 text-text-secondary">{state.routine.descripcion}</p>
          ) : null}
          <div className="mt-9 border-t border-border/60">
            {[...state.routine.dias].sort((first, second) => first.orden - second.orden).map((day) => {
              const expanded = expandedDayId === day.id;
              const contentId = `routine-day-${day.id}`;

              return (
                <section aria-label={day.nombre} className="min-w-0 border-b border-border/60" key={day.id}>
                  <button
                    aria-controls={contentId}
                    aria-expanded={expanded}
                    className={`flex min-h-14 w-full min-w-0 items-center gap-3 rounded-control-sm px-2 py-2 text-left transition-colors focus-visible:outline-2 focus-visible:outline-focus ${expanded ? "bg-surface-active/35" : "hover:bg-surface-interactive/45"}`}
                    onClick={() => setExpandedDayId((current) => current === day.id ? null : day.id)}
                    type="button"
                  >
                    <h2 className="min-w-0 flex-1 truncate font-brand text-base font-bold text-text-primary">{day.nombre}</h2>
                    <span className="shrink-0 text-[0.8125rem] font-semibold text-info">
                      {day.ejercicios.length} {day.ejercicios.length === 1 ? "ejercicio" : "ejercicios"}
                    </span>
                    <ChevronDown aria-hidden="true" className={`shrink-0 transition-[color,transform] motion-reduce:transition-none ${expanded ? "rotate-180 text-thermal-accent" : "text-text-muted"}`} size={16} strokeWidth={1.75} />
                  </button>

                  <div className="pb-6" hidden={!expanded} id={contentId}>
                    {day.descripcion?.trim() ? <p className="mb-3 text-sm leading-6 text-text-secondary">{day.descripcion}</p> : null}
                    {day.ejercicios.length === 0 ? (
                      <p className="text-sm leading-6 text-text-secondary">Todavía no hay ejercicios.</p>
                    ) : (
                      <ul className="divide-y divide-border/40 border-y border-border/40">
                        {[...day.ejercicios].sort((first, second) => first.orden - second.orden).map((exercise) => (
                          <li className="min-w-0 py-4" key={`${exercise.ejercicioId}-${exercise.orden}`}>
                            <h3 className="break-words font-brand text-base font-bold leading-5 text-text-primary">{exercise.nombre}</h3>
                            <RoutineExerciseTarget className="mt-1.5" {...exercise} />
                            {exercise.notas?.trim() ? <p className="mt-1 text-[0.8125rem] leading-5 text-text-secondary/85">{exercise.notas}</p> : null}
                          </li>
                        ))}
                      </ul>
                    )}
                    {day.ejercicios.length > 0 ? (
                      <div className="mt-7 sm:max-w-xs">
                        <Button disabled={Boolean(startingDayId)} fullWidth isLoading={startingDayId === day.id} onClick={() => void handleStart(day.id)} size="md">
                          {startingDayId === day.id ? "Iniciando..." : `Iniciar ${day.nombre}`}
                        </Button>
                        {state.routine.activa ? (
                          <Link
                            className="mt-2 flex min-h-11 w-full items-center justify-center gap-2 rounded-control-sm px-3 text-sm font-semibold text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
                            href={`/coach/adaptive-training/${encodeURIComponent(state.routine.id)}/${encodeURIComponent(day.id)}`}
                          >
                            <SlidersHorizontal aria-hidden="true" className="text-steel" size={17} />
                            Adaptar hoy
                          </Link>
                        ) : null}
                      </div>
                    ) : null}
                    {startError?.dayId === day.id ? (
                      <div className="mt-3 text-sm" role="alert">
                        <p className="text-text-secondary">
                          {startError.kind === "verification"
                            ? "No pudimos verificar si ya tenés un entrenamiento en curso. Intentá nuevamente."
                            : "No pudimos iniciar el entrenamiento. Intentá nuevamente."}
                        </p>
                      </div>
                    ) : null}
                  </div>
                </section>
              );
            })}
          </div>
        </>
      )}
      {recoverySession ? (
        <ActiveSessionRecoveryDialog
          onClose={() => setRecoverySession(null)}
          onResolved={() => {
            setRecoverySession(null);
            setStartNotice("Entrenamiento cancelado. Ya podés iniciar esta sesión.");
          }}
          onUnauthenticated={handleUnauthenticated}
          session={recoverySession}
        />
      ) : null}
    </section>
  );
}

function RoutineDetailSkeleton() {
  return (
    <div aria-label="Cargando rutina" className="mt-6 animate-pulse" role="status">
      <div className="h-4 w-40 rounded bg-surface-elevated" />
      <div className="mt-8 border-y border-border/50 py-4">
        <div className="h-5 w-32 rounded bg-surface-elevated" />
        <div className="mt-4 h-4 w-4/5 rounded bg-surface-elevated" />
        <div className="mt-3 h-4 w-3/5 rounded bg-surface-elevated" />
        <div className="mt-5 h-12 w-full rounded-control-sm bg-surface-elevated" />
      </div>
    </div>
  );
}
