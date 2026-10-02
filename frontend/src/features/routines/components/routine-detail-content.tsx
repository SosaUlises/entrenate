"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ChevronDown } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { startTrainingSessionAction } from "@/features/training/actions/training.actions";
import { getRoutineDetailAction, type GetRoutineDetailActionResult } from "../actions/get-routine-detail.action";

type DetailState = { status: "loading" } | GetRoutineDetailActionResult;

export function RoutineDetailContent({ id }: { id: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<DetailState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [expandedDayId, setExpandedDayId] = useState<string | null>(null);
  const [startingDayId, setStartingDayId] = useState<string | null>(null);
  const [startError, setStartError] = useState<{ dayId: string; conflict: boolean } | null>(null);

  async function handleStart(dayId: string) {
    if (startingDayId) return;
    setStartingDayId(dayId);
    setStartError(null);
    try {
      const result = await startTrainingSessionAction(dayId);
      if (result.status === "success") {
        router.push("/training");
        return;
      }
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      setStartError({ dayId, conflict: result.status === "conflict" });
    } catch {
      setStartError({ dayId, conflict: false });
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
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary"
          href="/routines"
        >
          <ArrowLeft aria-hidden="true" size={20} />
        </Link>
        <h1 className="min-w-0 truncate font-brand text-2xl font-bold text-text-primary" id="routine-detail-title" title={routine?.nombre}>
          {routine?.nombre ?? "Rutina"}
        </h1>
        {routine ? (
          <Link className="ml-auto inline-flex min-h-11 shrink-0 items-center rounded-control-sm px-2 text-sm font-semibold text-text-secondary hover:text-primary focus-visible:outline-primary" href={`/routines/${routine.id}/edit`}>
            Editar
          </Link>
        ) : null}
      </div>
      {routine ? (
        <p className="mt-1 pl-12 text-sm text-text-secondary">
          {routine.dias.length} {routine.dias.length === 1 ? "día" : "días"} · {exerciseCount} {exerciseCount === 1 ? "ejercicio" : "ejercicios"}
        </p>
      ) : null}

      {state.status === "loading" || state.status === "unauthenticated" ? (
        <div className="flex min-h-48 items-center justify-center">
          <Spinner className="size-6 text-primary" label="Cargando rutina" />
        </div>
      ) : state.status === "not-found" ? (
        <div className="mt-8">
          <p className="text-sm text-text-primary">No encontramos esta rutina.</p>
          <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href="/routines">
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
          <div className="mt-8 border-t border-border/60">
            {[...state.routine.dias].sort((first, second) => first.orden - second.orden).map((day) => {
              const expanded = expandedDayId === day.id;
              const contentId = `routine-day-${day.id}`;

              return (
                <section aria-label={day.nombre} className="min-w-0 border-b border-border/60" key={day.id}>
                  <button
                    aria-controls={contentId}
                    aria-expanded={expanded}
                    className="flex min-h-14 w-full min-w-0 items-center gap-3 py-2 text-left focus-visible:outline-primary"
                    onClick={() => setExpandedDayId((current) => current === day.id ? null : day.id)}
                    type="button"
                  >
                    <h2 className="min-w-0 flex-1 truncate font-brand text-base font-bold text-text-primary">{day.nombre}</h2>
                    <span className="shrink-0 text-xs font-medium text-info/80">
                      {day.ejercicios.length} {day.ejercicios.length === 1 ? "ejercicio" : "ejercicios"}
                    </span>
                    <ChevronDown aria-hidden="true" className={`shrink-0 text-text-secondary/75 transition-transform motion-reduce:transition-none ${expanded ? "rotate-180" : ""}`} size={16} strokeWidth={1.75} />
                  </button>

                  <div className="pb-6" hidden={!expanded} id={contentId}>
                    {day.descripcion?.trim() ? <p className="mb-3 text-sm leading-6 text-text-secondary">{day.descripcion}</p> : null}
                    {day.ejercicios.length === 0 ? (
                      <p className="text-sm leading-6 text-text-secondary">Todavía no hay ejercicios.</p>
                    ) : (
                      <ul className="divide-y divide-border/40 border-y border-border/40">
                        {[...day.ejercicios].sort((first, second) => first.orden - second.orden).map((exercise) => {
                          const imageSrc = getExerciseImage(exercise.nombre);
                          return (
                            <li className="flex min-w-0 items-start gap-3 py-3" key={`${exercise.ejercicioId}-${exercise.orden}`}>
                              {imageSrc ? (
                                <Image alt="" className="size-12 shrink-0 rounded-control-sm object-cover" height={48} src={imageSrc} width={48} />
                              ) : null}
                              <div className="min-w-0 flex-1">
                                <h3 className="break-words font-brand text-base font-bold leading-5 text-text-primary">{exercise.nombre}</h3>
                                <p className="mt-1 break-words text-sm leading-5 text-text-secondary">
                                  {exercise.cantidadSeries}×{exercise.repeticionesMinimas}–{exercise.repeticionesMaximas} · RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoSegundos} s
                                </p>
                                {exercise.notas?.trim() ? <p className="mt-1 text-xs leading-5 text-text-secondary">{exercise.notas}</p> : null}
                              </div>
                            </li>
                          );
                        })}
                      </ul>
                    )}
                    {day.ejercicios.length > 0 ? (
                      <Button className="mt-5 min-h-[3.25rem] rounded-control-sm text-[0.9375rem] font-bold sm:max-w-sm" disabled={Boolean(startingDayId)} fullWidth isLoading={startingDayId === day.id} onClick={() => void handleStart(day.id)}>
                        {startingDayId === day.id ? "Iniciando..." : `Iniciar ${day.nombre}`}
                      </Button>
                    ) : null}
                    {startError?.dayId === day.id ? (
                      <div className="mt-3 text-sm" role="alert">
                        <p className="text-text-secondary">{startError.conflict ? "Ya tenés un entrenamiento en curso." : "No pudimos iniciar el entrenamiento. Intentá nuevamente."}</p>
                        {startError.conflict ? <Link className="mt-2 inline-flex min-h-11 items-center font-semibold text-primary" href="/training">Continuar entrenamiento</Link> : null}
                      </div>
                    ) : null}
                  </div>
                </section>
              );
            })}
          </div>
        </>
      )}
    </section>
  );
}
