"use client";

import Image from "next/image";
import dynamic from "next/dynamic";
import { Check, CircleHelp, ScanEye, X } from "lucide-react";
import { useEffect, useRef, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { hasEntrenateExerciseDemo } from "@/features/exercises/components/exercise-demo-registry";
import { upsertTrainingSetAction } from "../actions/training.actions";
import { getFirstMissingTarget, type TrainingTarget } from "../training-position";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";

const ExerciseMovementDemo = dynamic(
  () => import("@/features/exercises/components/exercise-movement-demo"),
  { ssr: false },
);

type RestState = {
  changesExercise: boolean;
  remaining: number;
  next: TrainingTarget;
};

function formatTime(seconds: number): string {
  return `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
}

export function GuidedTrainingView({ session, onDirtyChange, onFinish, onMissing, onSaved, onUnauthenticated, workoutGuideIds }: {
  session: TrainingSession;
  onDirtyChange: (isDirty: boolean) => void;
  onFinish: () => void;
  onMissing: () => void;
  onSaved: (exerciseId: string, set: TrainingSet) => void;
  onUnauthenticated: () => void;
  workoutGuideIds: Record<string, string>;
}) {
  const [rest, setRest] = useState<RestState | null>(null);
  const [showSavedNotice, setShowSavedNotice] = useState(false);
  const target = getFirstMissingTarget(session);

  useEffect(() => {
    if (!rest || rest.remaining === 0) return;
    const interval = window.setInterval(() => {
      setRest((current) => current ? { ...current, remaining: Math.max(0, current.remaining - 1) } : null);
    }, 1000);
    return () => window.clearInterval(interval);
  }, [rest]);

  useEffect(() => {
    if (!showSavedNotice) return;
    const timeout = window.setTimeout(() => setShowSavedNotice(false), 240);
    return () => window.clearTimeout(timeout);
  }, [showSavedNotice]);

  if (rest) {
    const nextImage = rest.changesExercise
      ? getExerciseImage(rest.next.exercise.nombre)
      : undefined;

    return (
      <div className="mt-5 min-w-0 w-full max-w-full text-center">
        <div aria-live="polite" className="flex h-5 items-center justify-center" role="status">
          {showSavedNotice ? (
            <p className="inline-flex items-center gap-1.5 text-xs font-semibold text-success">
              <Check aria-hidden="true" size={14} strokeWidth={2.25} />
              Serie registrada
            </p>
          ) : null}
        </div>
        <p className="mt-3 text-[0.8125rem] font-semibold leading-5 tracking-[0.14em] text-info">
          {rest.remaining > 0 ? "DESCANSO" : "DESCANSO TERMINADO"}
        </p>
        <p className="mt-3 font-brand text-[5rem] font-bold leading-none tabular-nums text-text-primary sm:text-[6.5rem]" role="timer">
          {formatTime(rest.remaining)}
        </p>
        <div className="mt-8 min-w-0 w-full max-w-full border-t border-border/50 pt-5 text-left">
          <div className="flex min-w-0 items-center justify-between gap-3">
            <p className="text-[0.8125rem] font-semibold leading-5 tracking-[0.12em] text-info">SIGUE</p>
            <p className="shrink-0 font-brand text-[0.8125rem] font-semibold leading-5 tabular-nums text-text-secondary">
              EJERCICIO {String(rest.next.exercisePosition).padStart(2, "0")} / {String(session.ejercicios.length).padStart(2, "0")}
            </p>
          </div>
          <div className="mt-3 flex min-w-0 max-w-full items-center gap-3">
            {nextImage ? (
              <Image alt="" className="size-14 shrink-0 rounded-control-sm object-cover" height={56} src={nextImage} width={56} />
            ) : null}
            <div className="min-w-0">
              <p className="break-words font-brand text-xl font-bold leading-tight text-text-primary">{rest.next.exercise.nombre}</p>
              <p className="mt-1 font-brand text-[0.9375rem] font-semibold leading-5 tabular-nums text-text-secondary">
                Serie {String(rest.next.setNumber).padStart(2, "0")} / {String(rest.next.exercise.seriesObjetivo).padStart(2, "0")}
              </p>
            </div>
          </div>
        </div>
        {rest.remaining > 0 ? (
          <button
            className="mt-6 inline-flex min-h-11 items-center justify-center rounded-control-sm px-3 text-sm font-semibold text-text-secondary hover:text-primary focus-visible:outline-primary"
            onClick={() => setRest(null)}
            type="button"
          >
            Omitir descanso
          </button>
        ) : (
          <Button className="mt-6" onClick={() => setRest(null)}>Continuar</Button>
        )}
      </div>
    );
  }

  if (!target) {
    const registeredSets = session.ejercicios.reduce((total, exercise) => total + exercise.series.length, 0);
    return (
      <div className="mt-5 rounded-card border border-border/60 bg-surface/50 p-5">
        <h2 className="font-brand text-lg font-bold text-text-primary">Entrenamiento listo</h2>
        <p className="mt-2 text-sm text-text-secondary">
          {session.ejercicios.length} {session.ejercicios.length === 1 ? "ejercicio" : "ejercicios"} · {registeredSets} {registeredSets === 1 ? "serie registrada" : "series registradas"}
        </p>
        <Button className="mt-5" fullWidth onClick={onFinish}>Finalizar entrenamiento</Button>
      </div>
    );
  }

  const { exercise, exercisePosition, setNumber } = target;
  const previousSet = exercise.series
    .filter((set) => set.completada && set.numeroSerie < setNumber)
    .sort((first, second) => second.numeroSerie - first.numeroSerie)[0];
  const image = getExerciseImage(exercise.nombre);

  function handleSaved(set: TrainingSet) {
    const next = getFirstMissingTarget(session, { exerciseId: exercise.id, set });
    onSaved(exercise.id, set);
    if (next) {
      setShowSavedNotice(true);
      setRest({
        changesExercise: next.exercise.id !== exercise.id,
        next,
        remaining: exercise.descansoObjetivoSegundos,
      });
    }
  }

  return (
    <div className="mt-3 min-w-0 w-full max-w-full">
      <p className="font-brand text-[0.8125rem] font-semibold leading-5 tracking-[0.12em] text-info">
        EJERCICIO {String(exercisePosition).padStart(2, "0")} / {String(session.ejercicios.length).padStart(2, "0")}
      </p>
      <div className="mt-2 flex min-w-0 max-w-full items-center gap-3">
        {image ? <Image alt="" className="size-16 shrink-0 rounded-control-sm object-cover" height={64} src={image} width={64} /> : null}
        <div className="min-w-0 flex-1">
          <h2 className="break-words font-brand text-2xl font-bold leading-[1.1] text-text-primary sm:text-[1.75rem]">{exercise.nombre}</h2>
          {hasEntrenateExerciseDemo(exercise.nombre) || workoutGuideIds[exercise.ejercicioId] ? (
            <GuidedTechniquePreview
              exerciseName={exercise.nombre}
              key={exercise.ejercicioId}
              workoutGuideId={workoutGuideIds[exercise.ejercicioId]}
            />
          ) : null}
        </div>
      </div>

      <GuidedSetForm
        exercise={exercise}
        key={`${exercise.id}:${setNumber}`}
        onDirtyChange={onDirtyChange}
        number={setNumber}
        onMissing={onMissing}
        onSaved={handleSaved}
        onUnauthenticated={onUnauthenticated}
        previousSet={previousSet}
        sessionId={session.id}
      />
    </div>
  );
}

function GuidedTechniquePreview({ exerciseName, workoutGuideId }: {
  exerciseName: string;
  workoutGuideId?: string;
}) {
  const [open, setOpen] = useState(false);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  return (
    <>
      <button
        className="mt-2 inline-flex min-h-11 items-center gap-2 rounded-control-sm bg-surface/60 px-3 text-[0.8125rem] font-semibold text-text-secondary transition-colors hover:bg-primary/8 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary"
        onClick={() => {
          setOpen(true);
          dialogRef.current?.showModal();
        }}
        ref={triggerRef}
        type="button"
      >
        <ScanEye aria-hidden="true" className="text-primary" size={16} strokeWidth={1.75} />
        Ver técnica
      </button>
      <dialog
        aria-labelledby="guided-technique-title"
        aria-modal="true"
        className="fixed inset-x-0 top-auto bottom-0 m-0 max-h-[90dvh] w-full max-w-[100vw] overscroll-contain overflow-x-hidden overflow-y-auto rounded-t-container border border-border/50 bg-surface-elevated p-4 pb-[calc(1rem+env(safe-area-inset-bottom))] text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:max-h-[calc(100dvh-2rem)] sm:w-[calc(100%-2rem)] sm:max-w-lg sm:rounded-container sm:p-5"
        onClick={(event) => {
          const bounds = event.currentTarget.getBoundingClientRect();
          if (
            event.clientX < bounds.left || event.clientX > bounds.right ||
            event.clientY < bounds.top || event.clientY > bounds.bottom
          ) event.currentTarget.close();
        }}
        onClose={() => {
          setOpen(false);
          triggerRef.current?.focus();
        }}
        ref={dialogRef}
      >
        <div className="sticky top-0 z-10 flex min-w-0 items-start justify-between gap-3 bg-surface-elevated pb-3">
          <div className="min-w-0 flex-1">
            <p className="text-xs font-semibold leading-4 tracking-[0.14em] text-info">TÉCNICA</p>
            <h2 className="mt-1 break-words font-brand text-2xl font-bold leading-tight text-text-primary" id="guided-technique-title">{exerciseName}</h2>
          </div>
          <button
            aria-label="Cerrar técnica"
            className="flex size-11 shrink-0 items-center justify-center rounded-full bg-background/35 text-text-secondary transition-colors hover:bg-primary/8 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary"
            onClick={() => dialogRef.current?.close()}
            type="button"
          >
            <X aria-hidden="true" size={20} />
          </button>
        </div>
        {open ? (
          <ExerciseMovementDemo
            exerciseName={exerciseName}
            workoutGuideId={workoutGuideId ?? null}
          />
        ) : null}
      </dialog>
    </>
  );
}

function GuidedSetForm({ exercise, number, onDirtyChange, onMissing, onSaved, onUnauthenticated, previousSet, sessionId }: {
  exercise: TrainingSessionExercise;
  number: number;
  onDirtyChange: (isDirty: boolean) => void;
  onMissing: () => void;
  onSaved: (set: TrainingSet) => void;
  onUnauthenticated: () => void;
  previousSet?: TrainingSet;
  sessionId: string;
}) {
  const [weight, setWeight] = useState(previousSet ? String(previousSet.peso) : "");
  const [reps, setReps] = useState("");
  const [rir, setRir] = useState("");
  const [showRirHelp, setShowRirHelp] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const pendingRef = useRef(false);
  const initialWeight = previousSet ? String(previousSet.peso) : "";
  const isDirty = weight !== initialWeight || reps !== "" || rir !== "";

  useEffect(() => {
    onDirtyChange(isDirty);
  }, [isDirty, onDirtyChange]);

  useEffect(() => () => onDirtyChange(false), [onDirtyChange]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pendingRef.current) return;

    const peso = Number(weight.replace(",", "."));
    const repeticiones = Number(reps);
    const rirValue = rir.trim() === "" ? null : Number(rir);
    if (weight.trim() === "" || !Number.isFinite(peso) || peso < 0 || peso > 1000 || Math.abs(Math.round(peso * 100) - peso * 100) > 1e-8) {
      setError("Ingresá un peso entre 0 y 1000 kg, con hasta dos decimales.");
      return;
    }
    if (!Number.isInteger(repeticiones) || repeticiones < 1 || repeticiones > 200) {
      setError("Ingresá entre 1 y 200 repeticiones.");
      return;
    }
    if (rirValue !== null && (!Number.isInteger(rirValue) || rirValue < 0 || rirValue > 5)) {
      setError("El RIR debe estar entre 0 y 5, o quedar vacío.");
      return;
    }

    const input: TrainingSetInput = { peso, repeticiones, rir: rirValue };
    pendingRef.current = true;
    setPending(true);
    setError("");
    try {
      const result = await upsertTrainingSetAction(sessionId, exercise.id, number, input);
      if (result.status === "success") {
        onDirtyChange(false);
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
      pendingRef.current = false;
      setPending(false);
    }
  }

  const metricInputClass = "h-12 min-w-0 w-full max-w-full bg-transparent p-0 font-brand! text-[2.5rem]! font-bold! leading-none! tabular-nums text-text-primary caret-primary placeholder:text-text-secondary/25 focus-visible:outline-none! focus-visible:outline-offset-0 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none";

  return (
    <form className="mt-3 min-w-0 w-full max-w-full" noValidate onSubmit={(event) => void save(event)}>
      <div>
        <p className="text-[0.8125rem] font-semibold leading-5 tracking-[0.12em] text-text-secondary">SERIE</p>
        <p className="mt-1 font-brand text-5xl font-bold leading-none tabular-nums text-text-primary">
          {String(number).padStart(2, "0")} <span className="text-2xl font-semibold text-text-secondary">/ {String(exercise.seriesObjetivo).padStart(2, "0")}</span>
        </p>
      </div>
      <div className="mt-3 border-b border-border/40 pb-3">
        <p className="text-[0.8125rem] font-semibold leading-5 tracking-[0.12em] text-text-secondary">OBJETIVO</p>
        <dl className="mt-2 grid min-w-0 grid-cols-3">
          <div className="min-w-0 pr-2">
            <dt className="text-[0.6875rem] font-semibold leading-4 tracking-[0.06em] text-info">REPETICIONES</dt>
            <dd className="mt-0.5 font-brand text-xl font-bold leading-6 tabular-nums text-text-primary">
              {exercise.repeticionesMinimasObjetivo}–{exercise.repeticionesMaximasObjetivo}
            </dd>
          </div>
          <div className="min-w-0 border-l border-border/50 px-3">
            <dt className="text-[0.6875rem] font-semibold leading-4 tracking-[0.06em] text-info">RIR</dt>
            <dd className="mt-0.5 font-brand text-xl font-bold leading-6 tabular-nums text-text-primary">
              {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo}
            </dd>
          </div>
          <div className="min-w-0 border-l border-border/50 pl-3">
            <dt className="text-[0.6875rem] font-semibold leading-4 tracking-[0.06em] text-info">DESCANSO</dt>
            <dd className="mt-0.5 font-brand text-xl font-bold leading-6 tabular-nums text-text-primary">
              {exercise.descansoObjetivoSegundos}<span className="ml-1 text-sm font-semibold text-text-secondary">s</span>
            </dd>
          </div>
        </dl>
        {exercise.notas?.trim() ? <p className="mt-2 text-xs leading-5 text-text-secondary">{exercise.notas}</p> : null}
      </div>

      <div className="mt-3 space-y-3">
        <div className="grid min-w-0 grid-cols-2 gap-2.5">
          <label className="group min-w-0 rounded-control-sm bg-surface/55 px-3 py-2.5 transition-colors focus-within:bg-primary/5">
            <span className="block text-xs font-semibold leading-4 tracking-[0.1em] text-text-secondary transition-colors group-focus-within:text-primary">PESO</span>
            <span className="mt-1 grid min-w-0 grid-cols-[minmax(0,1fr)_auto] items-baseline gap-2">
              <input className={metricInputClass} inputMode="decimal" max="1000" min="0" onChange={(event) => setWeight(event.target.value)} placeholder="0" step="0.01" type="number" value={weight} />
              <span aria-hidden="true" className="self-end pb-1.5 pr-0.5 text-[0.8125rem] font-semibold leading-none text-text-secondary">KG</span>
            </span>
          </label>
          <label className="group min-w-0 rounded-control-sm bg-surface/55 px-3 py-2.5 transition-colors focus-within:bg-primary/5">
            <span className="block text-xs font-semibold leading-4 tracking-[0.1em] text-text-secondary transition-colors group-focus-within:text-primary">REPETICIONES</span>
            <input className={`${metricInputClass} mt-1`} inputMode="numeric" max="200" min="1" onChange={(event) => setReps(event.target.value)} placeholder="0" step="1" type="number" value={reps} />
          </label>
        </div>
        <fieldset className="min-w-0 max-w-full">
          <legend className="sr-only">RIR opcional</legend>
          <div className="flex min-h-11 items-center gap-1">
            <p className="text-xs font-semibold leading-4 tracking-[0.1em] text-text-secondary">
              RIR <span className="font-normal normal-case tracking-normal">· opcional</span>
            </p>
            <button
              aria-controls="rir-help"
              aria-expanded={showRirHelp}
              aria-label="Qué significa RIR"
              className="flex size-11 items-center justify-center rounded-full text-text-secondary hover:text-info focus-visible:outline-primary"
              onClick={() => setShowRirHelp((current) => !current)}
              type="button"
            >
              <CircleHelp aria-hidden="true" size={17} strokeWidth={1.75} />
            </button>
          </div>
          {showRirHelp ? (
            <p className="mb-2 border-l-2 border-info/45 bg-info/5 px-3 py-2 text-sm leading-5 text-text-secondary" id="rir-help" role="note">
              RIR significa repeticiones en reserva: cuántas repeticiones más podrías hacer al terminar la serie. 0 significa ninguna; 2 significa que podrías hacer dos más.
            </p>
          ) : null}
          <div className="grid min-w-0 w-full grid-cols-6 gap-1.5" role="group" aria-label="Seleccionar RIR opcional">
            {[0, 1, 2, 3, 4, 5].map((value) => {
              const selected = rir === String(value);
              return (
                <button
                  aria-label={`RIR ${value}`}
                  aria-pressed={selected}
                  className={`min-h-11 min-w-0 rounded-control-sm font-brand text-base font-bold tabular-nums transition-colors focus-visible:outline-primary ${selected ? "bg-primary-strong text-text-primary" : "bg-surface/55 text-text-secondary hover:bg-surface-elevated hover:text-text-primary"}`}
                  key={value}
                  onClick={() => setRir(selected ? "" : String(value))}
                  type="button"
                >
                  {value}
                </button>
              );
            })}
          </div>
        </fieldset>
      </div>
      {previousSet ? (
        <p className="mt-3 text-xs text-text-secondary">Anterior · {previousSet.peso} kg · {previousSet.repeticiones} reps · RIR {previousSet.rir ?? "—"}</p>
      ) : null}
      {error ? <p className="mt-3 text-sm text-error" role="alert">{error}</p> : null}
      <Button className="mt-4 min-h-[3.25rem] rounded-control-sm text-[0.9375rem] font-bold active:scale-[0.99] active:bg-primary/90" fullWidth isLoading={pending} type="submit">Completar serie</Button>
    </form>
  );
}
