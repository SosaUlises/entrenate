"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
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
      setState(result);
    }, () => {
      if (active) setState({ status: "error" });
    });

    return () => { active = false; };
  }, [id, invalidateSession, requestKey, router]);

  const routine = state.status === "ready" ? state.routine : null;

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
        <h1 className="min-w-0 truncate font-brand text-xl font-bold text-text-primary" id="routine-detail-title" title={routine?.nombre}>
          {routine?.nombre ?? "Rutina"}
        </h1>
        {routine ? (
          <Link className="ml-auto inline-flex min-h-11 shrink-0 items-center rounded-control-sm px-2 text-sm font-semibold text-primary hover:bg-primary/10 focus-visible:outline-primary" href={`/routines/${routine.id}/edit`}>
            Editar
          </Link>
        ) : null}
      </div>

      {state.status === "loading" || state.status === "unauthenticated" ? (
        <div className="flex min-h-48 items-center justify-center">
          <Spinner className="size-6 text-primary" label="Cargando rutina" />
        </div>
      ) : state.status === "not-found" ? (
        <div className="mt-8 rounded-card border border-border bg-surface p-5">
          <p className="text-sm text-text-primary">No encontramos esta rutina.</p>
          <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href="/routines">
            Volver a mis rutinas
          </Link>
        </div>
      ) : state.status === "error" ? (
        <div className="mt-8 rounded-card border border-border bg-surface p-5">
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
          <div className="mt-7 space-y-4">
            {[...state.routine.dias].sort((first, second) => first.orden - second.orden).map((day) => {
              const positionLabel = `Día ${day.orden}`;
              const showPosition = day.nombre.trim().toLocaleLowerCase("es-AR") !== positionLabel.toLocaleLowerCase("es-AR");
              return (
                <section aria-label={showPosition ? `${positionLabel}: ${day.nombre}` : day.nombre} className="rounded-card border border-border/60 bg-surface/45 p-4" key={day.id}>
                  {showPosition ? <p className="text-xs font-semibold text-primary">{positionLabel}</p> : null}
                  <h2 className="font-brand text-base font-bold text-text-primary">{day.nombre}</h2>
                  {day.descripcion?.trim() ? <p className="mt-1 text-sm text-text-secondary">{day.descripcion}</p> : null}
                  {day.ejercicios.length === 0 ? (
                    <p className="mt-3 text-sm text-text-secondary">Sin ejercicios.</p>
                  ) : (
                    <ul className="mt-3 divide-y divide-border/50">
                      {[...day.ejercicios].sort((first, second) => first.orden - second.orden).map((exercise) => {
                        const imageSrc = getExerciseImage(exercise.nombre);
                        return (
                          <li className="flex min-w-0 items-start gap-3 py-3" key={`${exercise.ejercicioId}-${exercise.orden}`}>
                            {imageSrc ? (
                              <Image alt="" className="size-12 shrink-0 rounded-control-sm object-cover" height={48} src={imageSrc} width={48} />
                            ) : null}
                            <div className="min-w-0 flex-1">
                              <h3 className="font-brand text-sm font-bold text-text-primary">{exercise.nombre}</h3>
                              <p className="mt-1 text-xs text-text-secondary">{exercise.cantidadSeries} series · {exercise.repeticionesMinimas}–{exercise.repeticionesMaximas} reps</p>
                              <p className="mt-0.5 text-xs text-text-secondary">RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoSegundos} s</p>
                              {exercise.notas?.trim() ? <p className="mt-2 text-xs leading-5 text-text-secondary">{exercise.notas}</p> : null}
                            </div>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                  <Button className="mt-3 min-h-10" disabled={Boolean(startingDayId) || day.ejercicios.length === 0} isLoading={startingDayId === day.id} onClick={() => void handleStart(day.id)}>
                    {startingDayId === day.id ? "Iniciando..." : "Iniciar entrenamiento"}
                  </Button>
                  {startError?.dayId === day.id ? (
                    <div className="mt-3 text-sm" role="alert">
                      <p className="text-text-secondary">{startError.conflict ? "Ya tenés un entrenamiento en curso." : "No pudimos iniciar el entrenamiento. Intentá nuevamente."}</p>
                      {startError.conflict ? <Link className="mt-2 inline-flex min-h-11 items-center font-semibold text-primary" href="/training">Continuar entrenamiento</Link> : null}
                    </div>
                  ) : null}
                </section>
              );
            })}
          </div>
        </>
      )}
    </section>
  );
}
