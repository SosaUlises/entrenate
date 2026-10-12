"use client";

import dynamic from "next/dynamic";
import { Check, ChevronDown, CircleHelp, ScanEye, X } from "lucide-react";
import { useEffect, useRef, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { hasEntrenateExerciseDemo } from "@/features/exercises/components/exercise-demo-registry";
import { upsertTrainingSetAction } from "../actions/training.actions";
import { getFirstMissingTarget, type TrainingTarget } from "../training-position";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";
import { TrainingProgressRail } from "./training-progress-rail";

const ExerciseMovementDemo = dynamic(
  () => import("@/features/exercises/components/exercise-movement-demo"),
  { ssr: false },
);

type RestState = { remaining: number; next: TrainingTarget };
type RestAdjustmentFeedback = { amount: number; id: number };

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
    const timeout = window.setTimeout(() => setShowSavedNotice(false), 360);
    return () => window.clearTimeout(timeout);
  }, [showSavedNotice]);

  function adjustRest(seconds: number) {
    setRest((current) => current
      ? { ...current, remaining: Math.max(0, current.remaining + seconds) }
      : null);
  }

  if (rest) {
    return <RestView onAdjust={adjustRest} onContinue={() => setRest(null)} remaining={rest.remaining} session={session} showSavedNotice={showSavedNotice} target={rest.next} />;
  }

  if (!target) {
    const registeredSets = session.ejercicios.reduce((total, exercise) => total + exercise.series.length, 0);
    return (
      <div className="mt-3 min-w-0 w-full">
        <TrainingProgressRail session={session} />
        <div className="mt-7 border-t border-border/50 pt-6">
          <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary">Entrenamiento listo</h2>
          <p className="mt-2 text-sm leading-6 text-text-secondary">
            {session.ejercicios.length} {session.ejercicios.length === 1 ? "ejercicio" : "ejercicios"} · {registeredSets} {registeredSets === 1 ? "serie registrada" : "series registradas"}
          </p>
          <Button className="mt-6 min-h-[3.25rem]" fullWidth onClick={onFinish}>Finalizar entrenamiento</Button>
        </div>
      </div>
    );
  }

  const { exercise, exercisePosition, setNumber } = target;
  const previousSet = exercise.series
    .filter((set) => set.completada && set.numeroSerie < setNumber)
    .sort((first, second) => second.numeroSerie - first.numeroSerie)[0];

  function handleSaved(set: TrainingSet) {
    const next = getFirstMissingTarget(session, { exerciseId: exercise.id, set });
    onSaved(exercise.id, set);
    if (next) {
      setShowSavedNotice(true);
      setRest({ next, remaining: exercise.descansoObjetivoSegundos });
    }
  }

  return (
    <div className="mt-3 flex min-h-[calc(100dvh-7.5rem)] min-w-0 w-full flex-col">
      <TrainingProgressRail session={session} />
      <div className="mt-4 min-w-0">
        <p className="font-brand text-[0.8125rem] font-semibold leading-5 tracking-[0.1em] text-info">
          EJERCICIO {String(exercisePosition).padStart(2, "0")} / {String(session.ejercicios.length).padStart(2, "0")}
        </p>
        <div className="mt-1 flex min-w-0 items-start justify-between gap-3">
          <h2 className="min-w-0 break-words font-brand text-2xl font-bold leading-[1.12] text-text-primary sm:text-[1.75rem]">{exercise.nombre}</h2>
          {hasEntrenateExerciseDemo(exercise.nombre) || workoutGuideIds[exercise.ejercicioId] ? (
            <GuidedTechniquePreview exerciseName={exercise.nombre} key={exercise.ejercicioId} workoutGuideId={workoutGuideIds[exercise.ejercicioId]} />
          ) : null}
        </div>
      </div>

      <GuidedSetForm
        exercise={exercise}
        key={`${exercise.id}:${setNumber}`}
        number={setNumber}
        onDirtyChange={onDirtyChange}
        onMissing={onMissing}
        onSaved={handleSaved}
        onUnauthenticated={onUnauthenticated}
        previousSet={previousSet}
        sessionId={session.id}
      />
    </div>
  );
}

function RestView({ onAdjust, onContinue, remaining, session, showSavedNotice, target }: {
  onAdjust: (seconds: number) => void;
  onContinue: () => void;
  remaining: number;
  session: TrainingSession;
  showSavedNotice: boolean;
  target: TrainingTarget;
}) {
  const [feedback, setFeedback] = useState<RestAdjustmentFeedback | null>(null);
  const feedbackRef = useRef<HTMLSpanElement>(null);
  const feedbackSequenceRef = useRef(0);

  useEffect(() => {
    if (!feedback || !feedbackRef.current) return;

    const element = feedbackRef.current;
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const animation = prefersReducedMotion
      ? null
      : element.animate(
        [
          { opacity: 0, transform: "translateY(4px)" },
          { opacity: 1, transform: "translateY(0)", offset: 0.25 },
          { opacity: 0, transform: "translateY(-4px)" },
        ],
        { duration: 420, easing: "cubic-bezier(0.22, 1, 0.36, 1)" },
      );

    if (prefersReducedMotion) element.style.opacity = "1";
    const timeout = window.setTimeout(() => setFeedback(null), 420);

    return () => {
      animation?.cancel();
      element.style.opacity = "";
      window.clearTimeout(timeout);
    };
  }, [feedback]);

  function adjust(seconds: number) {
    onAdjust(seconds);
    feedbackSequenceRef.current += 1;
    setFeedback({ amount: seconds, id: feedbackSequenceRef.current });
  }

  return (
    <div className="mt-3 flex min-h-[calc(100dvh-7.5rem)] min-w-0 w-full flex-col text-center">
      <TrainingProgressRail session={session} />
      <div aria-live="polite" className="mt-3 flex h-5 items-center justify-center" role="status">
        {showSavedNotice ? (
          <p className="inline-flex items-center gap-1.5 text-[0.8125rem] font-semibold text-success">
            <Check aria-hidden="true" size={15} strokeWidth={2.25} />
            Serie registrada
          </p>
        ) : null}
      </div>
      <div className="flex flex-1 flex-col items-center justify-center py-4">
        <p aria-live="polite" className="text-[0.8125rem] font-semibold leading-5 tracking-[0.12em] text-info">
          {remaining > 0 ? "DESCANSO" : "DESCANSO TERMINADO"}
        </p>
        <p aria-label={`${remaining} segundos restantes de descanso`} className={`mt-3 font-brand text-[5.5rem] font-bold leading-none tabular-nums sm:text-[6.75rem] ${remaining > 0 && remaining <= 5 ? "text-warning" : "text-text-primary"}`} role="timer">
          {formatTime(remaining)}
        </p>
        <div className="mt-3 flex w-full max-w-xs items-center justify-center gap-6">
          <button
            aria-label="Restar 15 segundos al descanso"
            className="min-h-12 min-w-24 rounded-control-sm px-3 text-sm font-semibold text-text-secondary transition-colors duration-150 hover:bg-surface/55 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary disabled:cursor-not-allowed disabled:opacity-35 motion-reduce:transition-none"
            disabled={remaining === 0}
            onClick={() => adjust(-15)}
            type="button"
          >
            −15 s
          </button>
          <button
            aria-label="Agregar 15 segundos al descanso"
            className="min-h-12 min-w-24 rounded-control-sm px-3 text-sm font-semibold text-text-primary transition-colors duration-150 hover:bg-primary/8 hover:text-primary focus-visible:outline-2 focus-visible:outline-primary motion-reduce:transition-none"
            onClick={() => adjust(15)}
            type="button"
          >
            +15 s
          </button>
        </div>
        <div aria-live="polite" className="mt-1 flex h-5 items-center justify-center text-[0.8125rem] font-semibold text-primary">
          {feedback ? (
            <span className="opacity-0" key={feedback.id} ref={feedbackRef}>
              {feedback.amount > 0 ? "+15 s" : "−15 s"}
            </span>
          ) : null}
        </div>
      </div>
      <div className="min-w-0 border-t border-border/50 pt-4 text-left">
        <div className="flex min-w-0 items-center justify-between gap-3">
          <p className="text-[0.8125rem] font-semibold leading-5 tracking-[0.1em] text-info">SIGUE</p>
          <p className="shrink-0 font-brand text-[0.8125rem] font-semibold leading-5 tabular-nums text-text-secondary">
            EJERCICIO {String(target.exercisePosition).padStart(2, "0")} / {String(session.ejercicios.length).padStart(2, "0")}
          </p>
        </div>
        <p className="mt-2 break-words font-brand text-xl font-bold leading-tight text-text-primary">{target.exercise.nombre}</p>
        <p className="mt-1 font-brand text-[0.9375rem] font-semibold leading-5 tabular-nums text-text-secondary">
          Serie {String(target.setNumber).padStart(2, "0")} / {String(target.exercise.seriesObjetivo).padStart(2, "0")}
        </p>
        {remaining > 0 ? (
          <button className="mt-4 inline-flex min-h-12 w-full items-center justify-center rounded-control-sm border border-border/60 text-sm font-semibold text-text-secondary transition-colors hover:border-border-strong hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary" onClick={onContinue} type="button">
            Terminar descanso
          </button>
        ) : (
          <Button className="mt-4 min-h-[3.25rem]" fullWidth onClick={onContinue}>Continuar</Button>
        )}
      </div>
    </div>
  );
}

function GuidedTechniquePreview({ exerciseName, workoutGuideId }: { exerciseName: string; workoutGuideId?: string }) {
  const [open, setOpen] = useState(false);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  return (
    <>
      <button
        className="inline-flex min-h-12 shrink-0 items-center gap-2 rounded-control-sm px-2 text-[0.8125rem] font-semibold text-text-secondary transition-colors hover:bg-primary/8 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary"
        onClick={() => { setOpen(true); dialogRef.current?.showModal(); }}
        ref={triggerRef}
        type="button"
      >
        <ScanEye aria-hidden="true" className="text-primary" size={17} strokeWidth={1.75} />
        Ver técnica
      </button>
      <dialog
        aria-labelledby="guided-technique-title"
        aria-modal="true"
        className="fixed inset-x-0 top-auto bottom-0 m-0 max-h-[90dvh] w-full max-w-[100vw] overscroll-contain overflow-x-hidden overflow-y-auto rounded-t-container border border-border/50 bg-surface-elevated p-4 pb-[calc(1rem+env(safe-area-inset-bottom))] text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:max-h-[calc(100dvh-2rem)] sm:w-[calc(100%-2rem)] sm:max-w-lg sm:rounded-container sm:p-5"
        onClick={(event) => {
          const bounds = event.currentTarget.getBoundingClientRect();
          if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) event.currentTarget.close();
        }}
        onClose={() => { setOpen(false); triggerRef.current?.focus(); }}
        ref={dialogRef}
      >
        <div className="sticky top-0 z-10 flex min-w-0 items-start justify-between gap-3 bg-surface-elevated pb-3">
          <h2 className="min-w-0 flex-1 break-words font-brand text-2xl font-bold leading-tight text-text-primary" id="guided-technique-title">{exerciseName}</h2>
          <button aria-label="Cerrar técnica" className="flex size-12 shrink-0 items-center justify-center rounded-full bg-background/35 text-text-secondary transition-colors hover:bg-primary/8 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary" onClick={() => dialogRef.current?.close()} type="button">
            <X aria-hidden="true" size={20} />
          </button>
        </div>
        {open ? <ExerciseMovementDemo exerciseName={exerciseName} workoutGuideId={workoutGuideId ?? null} /> : null}
      </dialog>
    </>
  );
}

function CompletedSetsDisclosure({ exercise }: { exercise: TrainingSessionExercise }) {
  const completedSets = exercise.series
    .filter((set) => set.completada && set.numeroSerie >= 1 && set.numeroSerie <= exercise.seriesObjetivo)
    .sort((first, second) => first.numeroSerie - second.numeroSerie);

  if (completedSets.length === 0) return null;

  return (
    <details className="group border-b border-border/40">
      <summary className="flex min-h-12 cursor-pointer list-none items-center justify-between gap-3 text-[0.8125rem] font-semibold text-text-secondary focus-visible:outline-2 focus-visible:outline-primary [&::-webkit-details-marker]:hidden">
        <span>Series registradas</span>
        <span className="flex items-center gap-2 font-brand tabular-nums">
          {completedSets.length} / {exercise.seriesObjetivo}
          <ChevronDown aria-hidden="true" className="transition-transform group-open:rotate-180 motion-reduce:transition-none" size={17} />
        </span>
      </summary>
      <ul className="divide-y divide-border/35 pb-2">
        {completedSets.map((set) => (
          <li className="flex min-h-10 items-center justify-between gap-3 text-[0.8125rem]" key={set.numeroSerie}>
            <span className="font-brand font-semibold tabular-nums text-text-secondary">Serie {String(set.numeroSerie).padStart(2, "0")}</span>
            <span className="text-right tabular-nums text-text-primary">{set.peso} kg · {set.repeticiones} reps · RIR {set.rir ?? "—"}</span>
          </li>
        ))}
      </ul>
    </details>
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
  const errorId = `guided-set-${exercise.id}-${number}-error`;
  const hasWeightError = error.startsWith("Ingresá un peso");
  const hasRepsError = error.startsWith("Ingresá entre");

  useEffect(() => { onDirtyChange(isDirty); }, [isDirty, onDirtyChange]);
  useEffect(() => () => onDirtyChange(false), [onDirtyChange]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pendingRef.current) return;
    const peso = Number(weight.replace(",", "."));
    const repeticiones = Number(reps);
    const rirValue = rir.trim() === "" ? null : Number(rir);
    if (weight.trim() === "" || !Number.isFinite(peso) || peso < 0 || peso > 1000 || Math.abs(Math.round(peso * 100) - peso * 100) > 1e-8) { setError("Ingresá un peso entre 0 y 1000 kg, con hasta dos decimales."); return; }
    if (!Number.isInteger(repeticiones) || repeticiones < 1 || repeticiones > 200) { setError("Ingresá entre 1 y 200 repeticiones."); return; }
    if (rirValue !== null && (!Number.isInteger(rirValue) || rirValue < 0 || rirValue > 5)) { setError("El RIR debe estar entre 0 y 5, o quedar vacío."); return; }

    const input: TrainingSetInput = { peso, repeticiones, rir: rirValue };
    pendingRef.current = true;
    setPending(true);
    setError("");
    try {
      const result = await upsertTrainingSetAction(sessionId, exercise.id, number, input);
      if (result.status === "success") { onDirtyChange(false); onSaved(result.data); }
      else if (result.status === "unauthenticated") onUnauthenticated();
      else if (result.status === "not-found" || result.status === "conflict") onMissing();
      else setError("No pudimos guardar esta serie. Intentá nuevamente.");
    } catch {
      setError("No pudimos guardar esta serie. Intentá nuevamente.");
    } finally {
      pendingRef.current = false;
      setPending(false);
    }
  }

  const metricInputClass = "h-12 min-w-0 w-full bg-transparent p-0 font-brand! text-[2rem]! font-extrabold! leading-none! tabular-nums text-text-primary caret-primary placeholder:text-text-secondary/35 outline-none! focus:outline-none! focus-visible:outline-none! [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none";
  const metricFieldClass = "group min-h-20 min-w-0 border-b border-border/45 py-2.5 transition-colors duration-150 focus-within:bg-primary/5 motion-reduce:transition-none";

  return (
    <form className="mt-3 flex min-w-0 w-full flex-1 flex-col" noValidate onSubmit={(event) => void save(event)}>
      <div className="border-b border-border/40 pb-3">
        <p className="text-[0.8125rem] font-semibold leading-5 tracking-[0.1em] text-text-secondary">SERIE</p>
        <p className="mt-1 font-brand text-[2.75rem] font-bold leading-none tabular-nums text-text-primary">
          {String(number).padStart(2, "0")} <span className="text-xl font-semibold text-text-secondary">/ {String(exercise.seriesObjetivo).padStart(2, "0")}</span>
        </p>
        <p className="mt-3 text-[0.8125rem] font-semibold leading-5 tracking-[0.1em] text-text-secondary">OBJETIVO</p>
        <p className="mt-1 font-brand text-[0.9375rem] font-bold leading-6 tabular-nums text-text-primary">
          {exercise.repeticionesMinimasObjetivo}–{exercise.repeticionesMaximasObjetivo} REPS <span className="text-text-secondary">·</span> RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} <span className="text-text-secondary">·</span> {exercise.descansoObjetivoSegundos} S
        </p>
        {exercise.notas?.trim() ? (
          <details className="group mt-2">
            <summary className="flex min-h-12 cursor-pointer list-none items-center gap-2 text-[0.8125rem] font-medium text-text-secondary focus-visible:outline-2 focus-visible:outline-primary [&::-webkit-details-marker]:hidden">
              Ver indicaciones
              <ChevronDown aria-hidden="true" className="transition-transform group-open:rotate-180 motion-reduce:transition-none" size={16} />
            </summary>
            <p className="pb-2 text-[0.8125rem] leading-5 text-text-secondary">{exercise.notas}</p>
          </details>
        ) : null}
      </div>

      <CompletedSetsDisclosure exercise={exercise} />

      <div className="mt-auto pt-4">
        {previousSet ? <p className="mb-3 text-[0.8125rem] leading-5 text-text-secondary">Anterior · {previousSet.peso} kg · {previousSet.repeticiones} reps · RIR {previousSet.rir ?? "—"}</p> : null}
        <div className="grid min-w-0 grid-cols-2 border-t border-border/45">
          <label className={`${metricFieldClass} pr-3 ${hasWeightError ? "border-b-error" : ""}`}>
            <span className="block text-[0.8125rem] font-semibold leading-5 tracking-[0.08em] text-text-secondary transition-colors duration-150 group-focus-within:text-primary motion-reduce:transition-none">PESO</span>
            <span className="mt-1 grid min-w-0 grid-cols-[minmax(0,1fr)_auto] items-baseline gap-2">
              <input aria-describedby={hasWeightError ? errorId : undefined} aria-invalid={hasWeightError} className={metricInputClass} inputMode="decimal" max="1000" min="0" onChange={(event) => setWeight(event.target.value)} placeholder="—" step="0.01" type="number" value={weight} />
              <span aria-hidden="true" className="self-end pb-1 text-[0.8125rem] font-semibold leading-none text-text-secondary">KG</span>
            </span>
          </label>
          <label className={`${metricFieldClass} border-l border-l-border/45 pl-3 ${hasRepsError ? "border-b-error" : ""}`}>
            <span className="block text-[0.8125rem] font-semibold leading-5 tracking-[0.08em] text-text-secondary transition-colors duration-150 group-focus-within:text-primary motion-reduce:transition-none">REPETICIONES</span>
            <input aria-describedby={hasRepsError ? errorId : undefined} aria-invalid={hasRepsError} className={`${metricInputClass} mt-1`} inputMode="numeric" max="200" min="1" onChange={(event) => setReps(event.target.value)} placeholder="—" step="1" type="number" value={reps} />
          </label>
        </div>
        <fieldset className="mt-3 min-w-0">
          <legend className="sr-only">RIR opcional</legend>
          <div className="flex min-h-12 items-center gap-1">
            <p className="text-[0.8125rem] font-semibold leading-5 tracking-[0.08em] text-text-secondary">RIR <span className="font-normal normal-case tracking-normal">· opcional</span></p>
            <button aria-controls="rir-help" aria-expanded={showRirHelp} aria-label="Qué significa RIR" className="flex size-12 items-center justify-center rounded-full text-text-secondary hover:text-info focus-visible:outline-2 focus-visible:outline-primary" onClick={() => setShowRirHelp((current) => !current)} type="button">
              <CircleHelp aria-hidden="true" size={18} strokeWidth={1.75} />
            </button>
          </div>
          {showRirHelp ? <p className="mb-2 border-l-2 border-info/45 bg-info/5 px-3 py-2 text-sm leading-5 text-text-secondary" id="rir-help" role="note">RIR significa repeticiones en reserva: cuántas repeticiones más podrías hacer al terminar la serie. 0 significa ninguna; 2 significa que podrías hacer dos más.</p> : null}
          <div className="grid min-w-0 w-full grid-cols-6 gap-1.5" role="group" aria-label="Seleccionar RIR opcional">
            {[0, 1, 2, 3, 4, 5].map((value) => {
              const selected = rir === String(value);
              return <button aria-label={`RIR ${value}`} aria-pressed={selected} className={`min-h-12 min-w-0 rounded-control-sm font-brand text-base font-bold tabular-nums transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-primary motion-reduce:transition-none ${selected ? "bg-primary-strong text-text-primary" : "bg-surface/55 text-text-secondary hover:bg-surface-elevated hover:text-text-primary"}`} key={value} onClick={() => setRir(selected ? "" : String(value))} type="button">{value}</button>;
            })}
          </div>
        </fieldset>
        {error ? <p className="mt-3 text-sm leading-5 text-error" id={errorId} role="alert">{error}</p> : null}
        <Button className="mt-4 min-h-[3.25rem] rounded-control-sm text-[0.9375rem] font-bold duration-150 active:scale-[0.99] active:bg-primary/90 motion-reduce:transition-none" fullWidth isLoading={pending} type="submit">Completar serie</Button>
      </div>
    </form>
  );
}
