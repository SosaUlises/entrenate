"use client";

import { Check, ChevronDown, LoaderCircle, Save, Trash2 } from "lucide-react";
import Image from "next/image";
import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { upsertTrainingSetAction } from "../actions/training.actions";
import { getOrderedExercises } from "../training-position";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";

function getCompletedTargetCount(exercise: TrainingSessionExercise): number {
  return new Set(
    exercise.series
      .filter((set) => set.completada && set.numeroSerie >= 1 && set.numeroSerie <= exercise.seriesObjetivo)
      .map((set) => set.numeroSerie),
  ).size;
}

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
  return exercises.find((exercise) => getCompletedTargetCount(exercise) < exercise.seriesObjetivo)?.id ?? null;
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
      <header className="mt-4 min-w-0 max-w-full">
        <h2 className="text-sm font-semibold uppercase leading-5 tracking-[0.11em] text-info">Carga libre</h2>
        <p className="mt-1 text-[0.9375rem] leading-6 text-text-secondary">Registrá tus series a tu ritmo.</p>
      </header>

      <div className="mt-4 min-w-0 max-w-full border-t border-border/50">
        {orderedExercises.map((exercise) => {
          const image = getExerciseImage(exercise.nombre);
          const completedTargetCount = getCompletedTargetCount(exercise);
          const isComplete = completedTargetCount === exercise.seriesObjetivo;
          const isExpanded = expandedExerciseId === exercise.id;
          const firstPendingTargetNumber = getFirstPendingTargetNumber(exercise);
          const highestSaved = Math.max(0, ...exercise.series.map((set) => set.numeroSerie));
          const count = Math.max(exercise.seriesObjetivo + (extraCounts[exercise.id] ?? 0), highestSaved);
          const contentId = `free-exercise-${exercise.id}`;

          return (
            <section className={`min-w-0 max-w-full border-b border-border/50 transition-colors ${isExpanded ? "rounded-control-sm bg-surface/40" : ""}`} key={exercise.id}>
              <button
                aria-controls={contentId}
                aria-expanded={isExpanded}
                className={`flex min-h-20 w-full min-w-0 items-center gap-3 py-3 text-left focus-visible:outline-2 focus-visible:outline-primary ${isExpanded ? "px-2" : "px-1"}`}
                onClick={() => setExpandedExerciseId((current) => current === exercise.id ? null : exercise.id)}
                type="button"
              >
                {image ? <Image alt="" className="size-12 shrink-0 rounded-control-sm object-cover" height={48} src={image} width={48} /> : null}
                <span className="min-w-0 flex-1">
                  <span className="block break-words font-brand text-[1.1875rem] font-bold leading-6 text-text-primary">{exercise.nombre}</span>
                  <span className="mt-1 block text-sm leading-5 text-text-secondary">
                    {exercise.seriesObjetivo} {exercise.seriesObjetivo === 1 ? "serie" : "series"} · {exercise.repeticionesMinimasObjetivo}–{exercise.repeticionesMaximasObjetivo} reps · RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoObjetivoSegundos} s
                  </span>
                </span>
                <span className="flex shrink-0 items-center gap-2">
                  <span className={`inline-flex items-center gap-1 font-brand text-[1.0625rem] font-bold tabular-nums ${isComplete ? "text-success" : "text-info"}`}>
                    {isComplete ? <Check aria-hidden="true" size={17} strokeWidth={2.25} /> : null}
                    {completedTargetCount} / {exercise.seriesObjetivo}
                  </span>
                  <span className={`flex size-10 items-center justify-center rounded-control-sm transition-colors ${isExpanded ? "bg-primary/5 text-primary" : "bg-transparent text-text-secondary/80"}`}>
                    <ChevronDown aria-hidden="true" className={`transition-transform motion-reduce:transition-none ${isExpanded ? "rotate-180" : ""}`} size={20} strokeWidth={1.9} />
                  </span>
                </span>
              </button>

              <div className="min-w-0 px-2 pb-4" hidden={!isExpanded} id={contentId}>
                {exercise.notas?.trim() ? <p className="mb-3 text-xs leading-5 text-text-secondary">{exercise.notas}</p> : null}
                <div aria-hidden="true" className="grid min-w-0 grid-cols-[2.25rem_minmax(0,1.05fr)_minmax(0,.85fr)_minmax(0,.65fr)_2.75rem] gap-1 border-b border-border/50 pb-2 text-xs font-semibold leading-4 tracking-[0.06em] text-text-secondary">
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
                  className="mt-2 inline-flex min-h-11 items-center rounded-control-sm px-1 text-[0.9375rem] font-medium text-text-secondary transition-colors hover:text-primary focus-visible:outline-primary"
                  onClick={() => setExtraCounts((current) => ({
                    ...current,
                    [exercise.id]: Math.max(current[exercise.id] ?? 0, highestSaved - exercise.seriesObjetivo) + 1,
                  }))}
                  type="button"
                >
                  + Agregar serie
                </button>
              </div>
            </section>
          );
        })}
      </div>
      <div className="mt-6 border-t border-border/50 pt-4">
        <Button fullWidth onClick={onFinish}>Finalizar entrenamiento</Button>
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

  const fieldClass = "h-11 min-w-0 w-full max-w-full rounded-control-sm border border-border/70 bg-background/75 px-1 text-center font-brand text-[1.0625rem] font-bold tabular-nums text-text-primary caret-primary outline-none transition-colors focus:border-primary/65 focus:bg-surface focus-visible:outline-2 focus-visible:outline-primary focus-visible:outline-offset-1 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none";

  const rowStateClass = error
    ? "bg-error/5"
    : isDirty
      ? "bg-primary/5"
      : isNextPending
        ? "bg-info/5"
        : "";

  return (
    <form className={`min-w-0 border-b border-border/35 px-1 py-2 transition-colors last:border-b-0 ${rowStateClass}`} onSubmit={(event) => void save(event)}>
      <div className="grid min-w-0 grid-cols-[2.25rem_minmax(0,1.05fr)_minmax(0,.85fr)_minmax(0,.65fr)_2.75rem] items-center gap-1">
        <span aria-label={`${isExtra ? "Serie extra" : "Serie"} ${number}${isNextPending ? ", próxima pendiente" : ""}`} className={`font-brand text-base font-bold tabular-nums ${isExtra || isNextPending ? "text-info" : "text-text-secondary"}`} title={isExtra ? "Serie extra" : isNextPending ? "Próxima serie pendiente" : undefined}>
          {String(number).padStart(2, "0")}
        </span>
        <label className="min-w-0">
          <span className="sr-only">Peso de la serie {number} en kilogramos</span>
          <input className={fieldClass} inputMode="decimal" max="1000" min="0" onChange={(event) => setWeight(event.target.value)} step="0.01" type="number" value={weight} />
        </label>
        <label className="min-w-0">
          <span className="sr-only">Repeticiones de la serie {number}</span>
          <input className={fieldClass} inputMode="numeric" max="200" min="1" onChange={(event) => setReps(event.target.value)} step="1" type="number" value={reps} />
        </label>
        <label className="min-w-0">
          <span className="sr-only">RIR opcional de la serie {number}</span>
          <input className={fieldClass} inputMode="numeric" max="5" min="0" onChange={(event) => setRir(event.target.value)} step="1" type="number" value={rir} />
        </label>
        <span className="flex size-11 items-center justify-center">
          {pending ? (
            <button aria-label={`Guardando serie ${number}`} className="flex size-11 items-center justify-center rounded-control-sm bg-primary/10 text-primary" disabled type="submit">
              <LoaderCircle aria-hidden="true" className="animate-spin motion-reduce:animate-none" size={18} />
            </button>
          ) : isDirty ? (
            <button aria-label={`${isSaved ? "Guardar cambios de" : "Completar"} serie ${number}`} className="flex size-11 items-center justify-center rounded-control-sm bg-primary-strong text-text-primary transition-colors hover:bg-primary focus-visible:outline-primary" type="submit">
              <Save aria-hidden="true" size={18} strokeWidth={2} />
            </button>
          ) : isSaved ? (
            <span className="flex size-11 items-center justify-center rounded-control-sm bg-success/10 text-success" role="status">
              <Check aria-hidden="true" size={18} strokeWidth={2.25} />
              <span className="sr-only">Serie {number} guardada</span>
            </span>
          ) : (
            <span aria-hidden="true" className="text-text-secondary/40">—</span>
          )}
        </span>
      </div>
      {error ? <p className="mt-1.5 pl-9 text-xs leading-5 text-error" role="alert">{error}</p> : null}
      {canRemove ? (
        <button
          aria-label={`Quitar serie extra ${number}`}
          className="ml-auto mt-1 flex min-h-11 items-center gap-2 rounded-control-sm px-2 text-xs font-medium text-text-secondary transition-colors hover:text-error focus-visible:outline-primary disabled:opacity-50"
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
