"use client";

import { Check, ChevronDown, LoaderCircle, Save, Trash2 } from "lucide-react";
import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { upsertTrainingSetAction } from "../actions/training.actions";
import { getCompletedTargetSetCount, getOrderedExercises } from "../training-position";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";
import { TrainingProgressRail } from "./training-progress-rail";

function getFirstPendingTargetNumber(exercise: TrainingSessionExercise): number | null {
  const completedNumbers = new Set(
    exercise.series.filter((set) => set.completada).map((set) => set.numeroSerie),
  );

  for (let number = 1; number <= exercise.seriesObjetivo; number += 1) {
    if (!completedNumbers.has(number)) return number;
  }

  return null;
}

function getInitialExpandedExerciseId(exercises: TrainingSessionExercise[]): string | null {
  return exercises.find((exercise) => getCompletedTargetSetCount(exercise) < exercise.seriesObjetivo)?.id ?? null;
}

export function FreeTrainingView({ session, onDirtyChange, onFinish, onMissing, onSaved, onUnauthenticated }: {
  session: TrainingSession;
  onDirtyChange: (isDirty: boolean) => void;
  onFinish: () => void;
  onMissing: () => void;
  onSaved: (exerciseId: string, set: TrainingSet) => void;
  onUnauthenticated: () => void;
}) {
  const orderedExercises = useMemo(() => getOrderedExercises(session), [session]);
  const [expandedExerciseId, setExpandedExerciseId] = useState<string | null>(() =>
    getInitialExpandedExerciseId(getOrderedExercises(session)),
  );
  const [extraCounts, setExtraCounts] = useState<Record<string, number>>({});
  const [dirtyRows, setDirtyRows] = useState<Set<string>>(() => new Set());

  useEffect(() => {
    onDirtyChange(dirtyRows.size > 0);
  }, [dirtyRows, onDirtyChange]);

  useEffect(() => () => onDirtyChange(false), [onDirtyChange]);

  const handleRowDirty = useCallback((rowId: string, isDirty: boolean) => {
    setDirtyRows((current) => {
      if (isDirty === current.has(rowId)) return current;
      const next = new Set(current);
      if (isDirty) next.add(rowId);
      else next.delete(rowId);
      return next;
    });
  }, []);

  return (
    <div className="min-w-0 w-full max-w-full">
      <div className="mt-3">
        <TrainingProgressRail session={session} />
      </div>
      <header className="mt-4 min-w-0 max-w-full border-b border-border/45 pb-4">
        <h2 className="font-brand text-xl font-bold leading-tight text-text-primary">Carga libre</h2>
        <p className="mt-1 text-[0.9375rem] leading-6 text-text-secondary">Registrá tus series a tu ritmo.</p>
      </header>

      <div className="min-w-0 max-w-full">
        {orderedExercises.map((exercise) => {
          const completedTargetCount = getCompletedTargetSetCount(exercise);
          const isComplete = completedTargetCount === exercise.seriesObjetivo;
          const isExpanded = expandedExerciseId === exercise.id;
          const firstPendingTargetNumber = getFirstPendingTargetNumber(exercise);
          const highestSaved = Math.max(0, ...exercise.series.map((set) => set.numeroSerie));
          const count = Math.max(exercise.seriesObjetivo + (extraCounts[exercise.id] ?? 0), highestSaved);
          const contentId = `free-exercise-${exercise.id}`;

          return (
            <section className="min-w-0 max-w-full border-b border-border/50" key={exercise.id}>
              <button
                aria-controls={contentId}
                aria-expanded={isExpanded}
                className="group flex min-h-[5rem] w-full min-w-0 items-center gap-3 py-3 text-left transition-colors duration-150 hover:bg-surface/25 focus-visible:outline-2 focus-visible:outline-primary motion-reduce:transition-none"
                onClick={() => setExpandedExerciseId((current) => current === exercise.id ? null : exercise.id)}
                type="button"
              >
                <span className="min-w-0 flex-1">
                  <span className="block break-words font-brand text-[1.1875rem] font-bold leading-6 text-text-primary">{exercise.nombre}</span>
                  <span className="mt-1 block break-words text-[0.8125rem] leading-5 text-text-secondary">
                    {exercise.seriesObjetivo} {exercise.seriesObjetivo === 1 ? "serie" : "series"} · {exercise.repeticionesMinimasObjetivo}–{exercise.repeticionesMaximasObjetivo} reps · RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoObjetivoSegundos} s
                  </span>
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <span
                    aria-label={`${completedTargetCount} de ${exercise.seriesObjetivo} series objetivo completadas`}
                    className={`inline-flex items-center gap-1 font-brand text-[1.0625rem] font-bold tabular-nums ${isComplete ? "text-success" : "text-info/75"}`}
                  >
                    {isComplete ? <Check aria-hidden="true" size={17} strokeWidth={2.25} /> : null}
                    {completedTargetCount} / {exercise.seriesObjetivo}
                  </span>
                  <span className={`flex size-11 items-center justify-center rounded-control-sm transition-colors duration-150 motion-reduce:transition-none ${isExpanded ? "training-expanded-control" : "bg-transparent text-text-secondary/80 group-hover:text-text-primary"}`}>
                    <ChevronDown aria-hidden="true" className={`transition-transform duration-150 motion-reduce:transition-none ${isExpanded ? "rotate-180" : ""}`} size={20} strokeWidth={1.9} />
                  </span>
                </span>
              </button>

              <div
                aria-hidden={!isExpanded}
                className={`grid min-w-0 transition-[grid-template-rows,opacity] duration-200 motion-reduce:transition-none ${isExpanded ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0"}`}
                id={contentId}
                inert={!isExpanded}
              >
                <div className="min-h-0 min-w-0 overflow-hidden pb-4">
                {exercise.notas?.trim() ? <p className="mb-3 text-[0.8125rem] leading-5 text-text-secondary">{exercise.notas}</p> : null}
                <div aria-hidden="true" className="grid min-w-0 grid-cols-[2.75rem_minmax(0,1fr)_minmax(0,.8fr)_minmax(0,.62fr)_6.5rem] gap-1 border-b border-border/50 pb-2 text-[0.8125rem] font-semibold leading-4 tracking-[0.06em] text-text-secondary">
                  <span>SET</span>
                  <span className="text-center">KG</span>
                  <span className="text-center">REPS</span>
                  <span className="text-center">RIR</span>
                  <span />
                </div>
                <div className="min-w-0">
                  {Array.from({ length: count }, (_, index) => index + 1).map((number) => (
                    <SetRow
                      exercise={exercise}
                      canRemove={number > exercise.seriesObjetivo && number > highestSaved && number === count}
                      isExtra={number > exercise.seriesObjetivo}
                      isNextPending={number === firstPendingTargetNumber}
                      key={`${exercise.id}:${number}`}
                      number={number}
                      onDirtyChange={handleRowDirty}
                      onMissing={onMissing}
                      onRemove={() => setExtraCounts((current) => ({
                        ...current,
                        [exercise.id]: Math.max(
                          Math.max(current[exercise.id] ?? 0, highestSaved - exercise.seriesObjetivo) - 1,
                          highestSaved - exercise.seriesObjetivo,
                        ),
                      }))}
                      onSaved={(set) => onSaved(exercise.id, set)}
                      onUnauthenticated={onUnauthenticated}
                      saved={exercise.series.find((set) => set.numeroSerie === number)}
                      rowId={`${exercise.id}:${number}`}
                      sessionId={session.id}
                    />
                  ))}
                </div>
                <button
                  className="mt-2 inline-flex min-h-12 items-center rounded-control-sm px-1 text-[0.9375rem] font-semibold text-text-secondary transition-colors duration-150 hover:text-primary active:text-primary focus-visible:outline-primary motion-reduce:transition-none"
                  onClick={() => setExtraCounts((current) => ({
                    ...current,
                    [exercise.id]: Math.max(current[exercise.id] ?? 0, highestSaved - exercise.seriesObjetivo) + 1,
                  }))}
                  type="button"
                >
                  + Agregar serie
                </button>
                </div>
              </div>
            </section>
          );
        })}
      </div>
      <div className="mt-8 border-t border-border/50 pt-6">
        <Button className="training-primary-action min-h-[3.25rem]" fullWidth onClick={onFinish}>Finalizar entrenamiento</Button>
      </div>
    </div>
  );
}

function SetRow({ exercise, canRemove, isExtra, isNextPending, number, onDirtyChange, onMissing, onRemove, onSaved, onUnauthenticated, rowId, saved, sessionId }: {
  exercise: TrainingSessionExercise;
  canRemove: boolean;
  isExtra: boolean;
  isNextPending: boolean;
  number: number;
  onDirtyChange: (rowId: string, isDirty: boolean) => void;
  onMissing: () => void;
  onRemove: () => void;
  onSaved: (set: TrainingSet) => void;
  onUnauthenticated: () => void;
  rowId: string;
  saved?: TrainingSet;
  sessionId: string;
}) {
  const [weight, setWeight] = useState(saved ? String(saved.peso) : "");
  const [reps, setReps] = useState(saved ? String(saved.repeticiones) : "");
  const [rir, setRir] = useState(saved?.rir != null ? String(saved.rir) : "");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const initialWeight = saved ? String(saved.peso) : "";
  const initialReps = saved ? String(saved.repeticiones) : "";
  const initialRir = saved?.rir != null ? String(saved.rir) : "";
  const isDirty = weight !== initialWeight || reps !== initialReps || rir !== initialRir;
  const isSaved = Boolean(saved?.completada);
  const errorId = `free-set-${rowId.replace(":", "-")}-error`;
  const hasWeightError = error.startsWith("Ingresá un peso");
  const hasRepsError = error.startsWith("Ingresá entre");
  const hasRirError = error.startsWith("El RIR");

  useEffect(() => {
    onDirtyChange(rowId, isDirty);
  }, [isDirty, onDirtyChange, rowId]);

  useEffect(() => () => onDirtyChange(rowId, false), [onDirtyChange, rowId]);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (pending) return;

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
    setPending(true);
    setError("");
    try {
      const result = await upsertTrainingSetAction(sessionId, exercise.id, number, input);
      if (result.status === "success") {
        setWeight(String(result.data.peso));
        setReps(String(result.data.repeticiones));
        setRir(result.data.rir === null ? "" : String(result.data.rir));
        onDirtyChange(rowId, false);
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

  const fieldClass = "training-ledger-input h-12 min-w-0 w-full max-w-full border-0 border-b border-border/35 bg-transparent px-1 text-center font-brand text-xl font-bold tabular-nums text-text-primary placeholder:text-text-secondary/35 outline-none! transition-colors duration-150 focus:outline-none! focus-visible:outline-none! motion-reduce:transition-none [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none";

  const rowStateClass = error
    ? "bg-error/5"
    : isDirty
      ? "training-ledger-current"
      : isNextPending
        ? "training-ledger-current"
        : "";

  return (
    <form className={`relative min-w-0 border-b border-border/35 px-1 py-2 transition-colors duration-150 last:border-b-0 motion-reduce:transition-none ${rowStateClass}`} onSubmit={(event) => void save(event)}>
      <div className="grid min-w-0 grid-cols-[2.75rem_minmax(0,1fr)_minmax(0,.8fr)_minmax(0,.62fr)_6.5rem] items-center gap-1">
        <span aria-label={`${isExtra ? "Serie extra" : "Serie"} ${number}${isNextPending ? ", próxima pendiente" : ""}`} className={`font-brand text-base font-bold tabular-nums ${isExtra || isNextPending ? "text-info/75" : "text-text-secondary"}`} title={isExtra ? "Serie extra" : isNextPending ? "Próxima serie pendiente" : undefined}>
          <span className="block">{String(number).padStart(2, "0")}</span>
          {isExtra ? <span className="mt-0.5 block font-sans text-[0.8125rem] font-semibold leading-4 text-info/75">Extra</span> : null}
        </span>
        <label className="min-w-0">
          <span className="sr-only">Peso de la serie {number} en kilogramos</span>
          <input aria-describedby={hasWeightError ? errorId : undefined} aria-invalid={hasWeightError} className={`${fieldClass} ${hasWeightError ? "border-b-error" : ""}`} inputMode="decimal" max="1000" min="0" onChange={(event) => setWeight(event.target.value)} placeholder="—" step="0.01" type="number" value={weight} />
        </label>
        <label className="min-w-0">
          <span className="sr-only">Repeticiones de la serie {number}</span>
          <input aria-describedby={hasRepsError ? errorId : undefined} aria-invalid={hasRepsError} className={`${fieldClass} ${hasRepsError ? "border-b-error" : ""}`} inputMode="numeric" max="200" min="1" onChange={(event) => setReps(event.target.value)} placeholder="—" step="1" type="number" value={reps} />
        </label>
        <label className="min-w-0">
          <span className="sr-only">RIR opcional de la serie {number}</span>
          <input aria-describedby={hasRirError ? errorId : undefined} aria-invalid={hasRirError} className={`${fieldClass} ${hasRirError ? "border-b-error" : ""}`} inputMode="numeric" max="5" min="0" onChange={(event) => setRir(event.target.value)} placeholder="—" step="1" type="number" value={rir} />
        </label>
        <span className="flex min-h-12 min-w-0 items-center justify-end">
          {pending ? (
            <button aria-label={`Guardando serie ${number}`} className="training-save-action flex min-h-12 w-full items-center justify-center gap-1.5 rounded-control-sm px-2 text-[0.8125rem] font-semibold" disabled type="submit">
              <LoaderCircle aria-hidden="true" className="animate-spin motion-reduce:animate-none" size={18} />
              <span>Guardando</span>
            </button>
          ) : isDirty ? (
            <button aria-label={`${isSaved ? "Guardar cambios de" : "Completar"} serie ${number}`} className="training-save-action flex min-h-12 w-full items-center justify-center gap-1.5 rounded-control-sm px-2 text-[0.8125rem] font-semibold transition-[background-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-primary motion-reduce:transition-none motion-reduce:active:scale-100" type="submit">
              <Save aria-hidden="true" size={16} strokeWidth={2} />
              <span>Guardar</span>
            </button>
          ) : isSaved ? (
            <span className="flex min-h-12 w-full items-center justify-center gap-2 rounded-control-sm px-2 text-sm font-semibold text-success transition-[color,opacity,transform] duration-200 starting:scale-[0.98] starting:opacity-0 motion-reduce:transition-none" role="status">
              <Check aria-hidden="true" size={18} strokeWidth={2.25} />
              <span>Guardada</span>
              <span className="sr-only">: serie {number}</span>
            </span>
          ) : (
            <span aria-hidden="true" className="text-text-secondary/40">—</span>
          )}
        </span>
      </div>
      {error ? <p className="mt-1.5 pl-9 text-[0.8125rem] leading-5 text-error" id={errorId} role="alert">{error}</p> : null}
      {canRemove ? (
        <button
          aria-label={`Quitar serie extra ${number}`}
          className="ml-auto mt-1 flex min-h-12 items-center gap-2 rounded-control-sm px-2 text-[0.8125rem] font-semibold text-text-secondary transition-[background-color,color,transform] duration-150 hover:bg-error/5 hover:text-error active:scale-[0.99] active:text-error focus-visible:bg-error/5 focus-visible:text-error focus-visible:outline-primary disabled:opacity-50 motion-reduce:transition-none motion-reduce:active:scale-100"
          disabled={pending}
          onClick={onRemove}
          type="button"
        >
          <Trash2 aria-hidden="true" size={15} strokeWidth={1.75} />
          Quitar serie
        </button>
      ) : null}
    </form>
  );
}
