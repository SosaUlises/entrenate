"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  cancelTrainingSessionAction, completeTrainingSessionAction,
  getActiveTrainingSessionAction, upsertTrainingSetAction,
} from "../actions/training.actions";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";

type ViewState = { status: "loading" | "empty" | "error" } | { status: "ready"; session: TrainingSession };
type FinishMode = "complete" | "cancel";

function formatTime(seconds: number): string {
  return `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
}

export function ActiveTrainingContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [view, setView] = useState<ViewState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [extraCounts, setExtraCounts] = useState<Record<string, number>>({});
  const [restRemaining, setRestRemaining] = useState<number | null>(null);
  const [finishMode, setFinishMode] = useState<FinishMode | null>(null);
  const [finishPending, setFinishPending] = useState(false);
  const [finishError, setFinishError] = useState(false);
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    let active = true;
    void getActiveTrainingSessionAction().then((result) => {
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else if (result.status === "success") {
        setView({ status: "ready", session: result.data });
      } else {
        setView({ status: result.status === "not-found" ? "empty" : "error" });
      }
    }, () => { if (active) setView({ status: "error" }); });
    return () => { active = false; };
  }, [invalidateSession, requestKey, router]);

  useEffect(() => {
    if (restRemaining === null || restRemaining === 0) return;
    const interval = window.setInterval(() => setRestRemaining((remaining) => remaining === null ? null : Math.max(0, remaining - 1)), 1000);
    return () => window.clearInterval(interval);
  }, [restRemaining]);

  function reload() {
    setView({ status: "loading" });
    setExtraCounts({});
    setRestRemaining(null);
    setRequestKey((key) => key + 1);
  }

  function openFinish(mode: FinishMode) {
    setFinishError(false);
    setFinishMode(mode);
    dialogRef.current?.showModal();
  }

  async function finish() {
    if (view.status !== "ready" || !finishMode || finishPending) return;
    setFinishPending(true);
    setFinishError(false);
    try {
      const result = finishMode === "complete"
        ? await completeTrainingSessionAction(view.session.id)
        : await cancelTrainingSessionAction(view.session.id);
      if (result.status === "success") {
        dialogRef.current?.close();
        router.replace("/home");
      } else if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else if (result.status === "not-found" || result.status === "conflict") {
        dialogRef.current?.close();
        reload();
      } else {
        setFinishError(true);
      }
    } catch {
      setFinishError(true);
    } finally {
      setFinishPending(false);
    }
  }

  function recordSet(exerciseId: string, set: TrainingSet, restSeconds: number) {
    setView((current) => current.status === "ready" ? {
      status: "ready",
      session: {
        ...current.session,
        ejercicios: current.session.ejercicios.map((exercise) => exercise.id === exerciseId ? {
          ...exercise,
          series: [...exercise.series.filter((item) => item.numeroSerie !== set.numeroSerie), set],
        } : exercise),
      },
    } : current);
    if (restSeconds > 0) {
      setRestRemaining(restSeconds);
    }
  }

  const session = view.status === "ready" ? view.session : null;
  const remaining = restRemaining;
  const hasIncompleteTargets = session?.ejercicios.some((exercise) =>
    Array.from({ length: exercise.seriesObjetivo }, (_, index) => index + 1)
      .some((number) => !exercise.series.some((set) => set.numeroSerie === number && set.completada))) ?? false;

  return (
    <section className="mx-auto w-full max-w-xl" aria-labelledby="training-title">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="font-brand text-xl font-bold text-text-primary" id="training-title">Entrenamiento en curso</h1>
          {session?.diaRutinaId ? <p className="mt-1 text-xs text-text-secondary">Día de rutina</p> : null}
        </div>
        {session ? <button className="min-h-11 px-2 text-sm text-text-secondary hover:text-text-primary focus-visible:outline-primary" onClick={() => openFinish("cancel")} type="button">Cancelar</button> : null}
      </div>

      {view.status === "loading" ? <div className="grid min-h-48 place-items-center"><Spinner className="size-6 text-primary" label="Cargando entrenamiento" /></div> : null}
      {view.status === "empty" ? <div className="mt-6 rounded-card border border-border bg-surface p-5"><p className="text-sm text-text-primary">No tenés un entrenamiento activo.</p><Link className="mt-3 inline-flex min-h-11 items-center text-sm font-semibold text-primary" href="/routines">Ver mis rutinas</Link></div> : null}
      {view.status === "error" ? <div className="mt-6 rounded-card border border-border bg-surface p-5"><p className="text-sm text-text-primary">No pudimos cargar tu entrenamiento.</p><Button className="mt-4" onClick={reload} variant="secondary">Reintentar</Button></div> : null}

      {session ? (
        <>
          {remaining !== null ? <div aria-live="polite" className="mt-5 flex items-center justify-between rounded-control border border-primary/20 bg-primary/10 px-4 py-2 text-sm text-primary"><span>{remaining > 0 ? `Descanso · ${formatTime(remaining)}` : "Descanso terminado"}</span><button className="min-h-11 px-2 font-semibold focus-visible:outline-primary" onClick={() => setRestRemaining(null)} type="button">Omitir</button></div> : null}
          <div className="mt-5 space-y-5">
            {[...session.ejercicios].sort((a, b) => a.orden - b.orden).map((exercise) => {
              const image = getExerciseImage(exercise.nombre);
              const highestSaved = Math.max(0, ...exercise.series.map((set) => set.numeroSerie));
              const count = Math.max(exercise.seriesObjetivo + (extraCounts[exercise.id] ?? 0), highestSaved);
              return <section className="rounded-card border border-border/70 bg-surface/65 p-4" key={exercise.id}>
                <div className="flex items-start gap-3">
                  {image ? <Image alt="" className="size-14 shrink-0 rounded-control-sm object-cover" height={56} src={image} width={56} /> : null}
                  <div className="min-w-0">
                    <h2 className="font-brand text-base font-bold text-text-primary">{exercise.nombre}</h2>
                    <p className="mt-1 text-xs text-text-secondary">Objetivo · {exercise.seriesObjetivo} series · {exercise.repeticionesMinimasObjetivo}–{exercise.repeticionesMaximasObjetivo} reps</p>
                    <p className="mt-0.5 text-xs text-text-secondary">RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoObjetivoSegundos} s</p>
                    {exercise.notas?.trim() ? <p className="mt-2 text-xs leading-5 text-text-secondary">{exercise.notas}</p> : null}
                  </div>
                </div>
                <div className="mt-4 space-y-3">
                  {Array.from({ length: count }, (_, index) => index + 1).map((number) => (
                    <SetRow
                      exercise={exercise}
                      key={`${exercise.id}-${number}`}
                      number={number}
                      onMissing={reload}
                      onSaved={(set) => recordSet(exercise.id, set, exercise.descansoObjetivoSegundos)}
                      onUnauthenticated={() => { invalidateSession(); router.replace("/login"); }}
                      saved={exercise.series.find((set) => set.numeroSerie === number)}
                      sessionId={session.id}
                    />
                  ))}
                </div>
                <button className="mt-3 min-h-11 text-sm font-semibold text-primary focus-visible:outline-primary" onClick={() => setExtraCounts((current) => ({ ...current, [exercise.id]: Math.max(current[exercise.id] ?? 0, highestSaved - exercise.seriesObjetivo) + 1 }))} type="button">+ Agregar serie</button>
              </section>;
            })}
          </div>
          <Button className="mt-7" fullWidth onClick={() => openFinish("complete")}>Finalizar entrenamiento</Button>
        </>
      ) : null}

      <dialog aria-labelledby="finish-title" aria-modal="true" className="m-auto w-[calc(100%-2rem)] max-w-sm rounded-card border border-border bg-surface-elevated p-5 text-text-primary shadow-elevated backdrop:bg-black/75" onClick={(event) => { if (event.target === event.currentTarget && !finishPending) event.currentTarget.close(); }} onClose={() => setFinishMode(null)} ref={dialogRef}>
        <h2 className="font-brand text-lg font-bold" id="finish-title">{finishMode === "cancel" ? "¿Cancelar entrenamiento?" : "¿Finalizar entrenamiento?"}</h2>
        <p className="mt-2 text-sm leading-6 text-text-secondary">{finishMode === "cancel" ? "La sesión se guardará como cancelada." : "Las series registradas quedarán guardadas en tu historial."}</p>
        {finishMode === "complete" && hasIncompleteTargets ? <p className="mt-2 text-sm text-warning">Todavía hay series sin completar.</p> : null}
        {finishError ? <p className="mt-3 text-sm text-error" role="alert">No pudimos actualizar el entrenamiento. Intentá nuevamente.</p> : null}
        <div className="mt-5 flex flex-wrap justify-end gap-2">
          <Button disabled={finishPending} onClick={() => dialogRef.current?.close()} variant="secondary">Seguir entrenando</Button>
          <Button isLoading={finishPending} onClick={() => void finish()}>{finishMode === "cancel" ? "Cancelar sesión" : "Finalizar"}</Button>
        </div>
      </dialog>
    </section>
  );
}

function SetRow({ exercise, number, onMissing, onSaved, onUnauthenticated, saved, sessionId }: {
  exercise: TrainingSessionExercise;
  number: number;
  onMissing: () => void;
  onSaved: (set: TrainingSet) => void;
  onUnauthenticated: () => void;
  saved?: TrainingSet;
  sessionId: string;
}) {
  const [weight, setWeight] = useState(saved ? String(saved.peso) : "");
  const [reps, setReps] = useState(saved ? String(saved.repeticiones) : "");
  const [rir, setRir] = useState(saved?.rir != null ? String(saved.rir) : "");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");

  async function save() {
    if (pending) return;
    const peso = Number(weight.replace(",", "."));
    const repeticiones = Number(reps);
    const rirValue = rir.trim() === "" ? null : Number(rir);
    if (weight.trim() === "" || !Number.isFinite(peso) || peso < 0 || peso > 1000 || Math.abs(Math.round(peso * 100) - peso * 100) > 1e-8) {
      setError("Ingresá un peso entre 0 y 1000 kg, con hasta dos decimales."); return;
    }
    if (!Number.isInteger(repeticiones) || repeticiones < 1 || repeticiones > 200) {
      setError("Ingresá entre 1 y 200 repeticiones."); return;
    }
    if (rirValue !== null && (!Number.isInteger(rirValue) || rirValue < 0 || rirValue > 5)) {
      setError("El RIR debe estar entre 0 y 5, o quedar vacío."); return;
    }
    const input: TrainingSetInput = { peso, repeticiones, rir: rirValue };
    setPending(true);
    setError("");
    try {
      const result = await upsertTrainingSetAction(sessionId, exercise.id, number, input);
      if (result.status === "success") {
        setWeight(String(result.data.peso));
        setReps(String(result.data.repeticiones));
        setRir(result.data.rir === null ? "" : String(result.data.rir));
        onSaved(result.data);
      } else if (result.status === "unauthenticated") {
        onUnauthenticated();
      } else if (result.status === "not-found" || result.status === "conflict") {
        onMissing();
      } else {
        setError("No pudimos guardar esta serie. Intentá nuevamente.");
      }
    } catch {
      setError("No pudimos guardar esta serie. Intentá nuevamente.");
    } finally {
      setPending(false);
    }
  }

  const fieldClass = "min-w-0 w-full min-h-11 rounded-control-sm border border-border bg-background px-2 text-center text-sm text-text-primary focus:border-primary focus:outline-none";
  return <div className="rounded-control border border-border/60 bg-background/40 p-3">
    <div className="flex items-center justify-between gap-2"><span className="text-xs font-semibold text-text-primary">Serie {number}</span>{saved?.completada ? <span className="text-xs font-semibold text-primary">Completada</span> : null}</div>
    <div className="mt-2 grid grid-cols-3 gap-2">
      <label className="min-w-0 text-xs text-text-secondary">Peso · kg<input className={`${fieldClass} mt-1`} inputMode="decimal" max="1000" min="0" onChange={(event) => setWeight(event.target.value)} step="0.01" type="number" value={weight} /></label>
      <label className="min-w-0 text-xs text-text-secondary">Reps<input className={`${fieldClass} mt-1`} inputMode="numeric" max="200" min="1" onChange={(event) => setReps(event.target.value)} step="1" type="number" value={reps} /></label>
      <label className="min-w-0 text-xs text-text-secondary">RIR<input className={`${fieldClass} mt-1`} inputMode="numeric" max="5" min="0" onChange={(event) => setRir(event.target.value)} step="1" type="number" value={rir} /></label>
    </div>
    {error ? <p className="mt-2 text-xs text-error" role="alert">{error}</p> : null}
    <button className="mt-2 min-h-11 text-sm font-semibold text-primary disabled:opacity-60 focus-visible:outline-primary" disabled={pending} onClick={() => void save()} type="button">{pending ? "Guardando..." : saved ? "Guardar cambios" : "Completar"}</button>
  </div>;
}
