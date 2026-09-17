"use client";

import Image from "next/image";
import { useEffect, useRef, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { upsertTrainingSetAction } from "../actions/training.actions";
import { getFirstMissingTarget, type TrainingTarget } from "../training-position";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";

type RestState = {
  remaining: number;
  next: TrainingTarget;
};

function formatTime(seconds: number): string {
  return `${String(Math.floor(seconds / 60)).padStart(2, "0")}:${String(seconds % 60).padStart(2, "0")}`;
}

export function GuidedTrainingView({ session, onFinish, onMissing, onSaved, onUnauthenticated }: {
  session: TrainingSession;
  onFinish: () => void;
  onMissing: () => void;
  onSaved: (exerciseId: string, set: TrainingSet) => void;
  onUnauthenticated: () => void;
}) {
  const [rest, setRest] = useState<RestState | null>(null);
  const target = getFirstMissingTarget(session);

  useEffect(() => {
    if (!rest || rest.remaining === 0) return;
    const interval = window.setInterval(() => {
      setRest((current) => current ? { ...current, remaining: Math.max(0, current.remaining - 1) } : null);
    }, 1000);
    return () => window.clearInterval(interval);
  }, [rest]);

  if (rest) {
    return (
      <div className="mt-10 flex flex-col items-center text-center">
        <p className="text-xs font-semibold tracking-wide text-primary">{rest.remaining > 0 ? "DESCANSO" : "Descanso terminado"}</p>
        <p className="mt-4 font-brand text-5xl font-bold tabular-nums text-text-primary" role="timer">{formatTime(rest.remaining)}</p>
        <div className="mt-8 w-full border-t border-border/60 pt-5">
          <p className="text-xs font-semibold text-text-secondary">Próxima</p>
          <p className="mt-2 font-brand text-lg font-bold text-text-primary">{rest.next.exercise.nombre}</p>
          <p className="mt-1 text-sm text-text-secondary">Serie {rest.next.setNumber} de {rest.next.exercise.seriesObjetivo}</p>
        </div>
        <Button className="mt-7" onClick={() => setRest(null)} variant={rest.remaining > 0 ? "secondary" : "primary"}>
          {rest.remaining > 0 ? "Omitir descanso" : "Continuar"}
        </Button>
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
      setRest({ next, remaining: exercise.descansoObjetivoSegundos });
    }
  }

  return (
    <div className="mt-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-primary/80">Ejercicio {exercisePosition} de {session.ejercicios.length}</p>
      <div className="mt-2 flex items-center gap-3">
        {image ? <Image alt="" className="size-16 shrink-0 rounded-control-sm object-cover" height={64} src={image} width={64} /> : null}
        <h2 className="min-w-0 font-brand text-2xl font-bold leading-tight text-text-primary">{exercise.nombre}</h2>
      </div>

      <GuidedSetForm
        exercise={exercise}
        key={`${exercise.id}:${setNumber}`}
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

function GuidedSetForm({ exercise, number, onMissing, onSaved, onUnauthenticated, previousSet, sessionId }: {
  exercise: TrainingSessionExercise;
  number: number;
  onMissing: () => void;
  onSaved: (set: TrainingSet) => void;
  onUnauthenticated: () => void;
  previousSet?: TrainingSet;
  sessionId: string;
}) {
  const [weight, setWeight] = useState(previousSet ? String(previousSet.peso) : "");
  const [reps, setReps] = useState("");
  const [rir, setRir] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState("");
  const pendingRef = useRef(false);

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

  const fieldClass = "min-h-12 min-w-0 w-full rounded-control border border-border bg-surface/50 px-3 text-center font-brand text-xl font-bold tabular-nums text-text-primary focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary/40";

  return (
    <form className="mt-5" noValidate onSubmit={(event) => void save(event)}>
      <div>
        <p className="text-xs font-semibold tracking-wide text-text-secondary">SERIE</p>
        <p className="mt-1 font-brand text-4xl font-bold leading-none tabular-nums text-text-primary">
          {String(number).padStart(2, "0")} <span className="text-xl font-semibold text-text-secondary">/ {String(exercise.seriesObjetivo).padStart(2, "0")}</span>
        </p>
      </div>
      <div className="mt-4">
        <p className="text-xs font-semibold tracking-wide text-text-secondary">OBJETIVO</p>
        <p className="mt-2 text-sm font-semibold leading-6 text-text-primary">
          {exercise.repeticionesMinimasObjetivo}–{exercise.repeticionesMaximasObjetivo} REPS · RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoObjetivoSegundos} S
        </p>
        {exercise.notas?.trim() ? <p className="mt-2 text-xs leading-5 text-text-secondary">{exercise.notas}</p> : null}
      </div>

      <div className="mt-5 space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <label className="block min-w-0">
            <span className="text-xs font-semibold tracking-wide text-text-secondary">PESO <span className="font-normal normal-case tracking-normal">· kg</span></span>
            <input className={`${fieldClass} mt-2`} inputMode="decimal" max="1000" min="0" onChange={(event) => setWeight(event.target.value)} step="0.01" type="number" value={weight} />
          </label>
          <label className="block min-w-0">
            <span className="text-xs font-semibold tracking-wide text-text-secondary">REPETICIONES</span>
            <input className={`${fieldClass} mt-2`} inputMode="numeric" max="200" min="1" onChange={(event) => setReps(event.target.value)} step="1" type="number" value={reps} />
          </label>
        </div>
        <label className="block w-1/2 min-w-0">
          <span className="text-xs font-semibold tracking-wide text-text-secondary">RIR <span className="font-normal normal-case tracking-normal">· opcional</span></span>
          <input className={`${fieldClass} mt-2`} inputMode="numeric" max="5" min="0" onChange={(event) => setRir(event.target.value)} step="1" type="number" value={rir} />
        </label>
      </div>
      {previousSet ? (
        <p className="mt-4 text-xs text-text-secondary">Anterior · {previousSet.peso} kg · {previousSet.repeticiones} reps · RIR {previousSet.rir ?? "—"}</p>
      ) : null}
      {error ? <p className="mt-3 text-sm text-error" role="alert">{error}</p> : null}
      <Button className="mt-5" fullWidth isLoading={pending} type="submit">Completar serie</Button>
    </form>
  );
}
