"use client";

import Image from "next/image";
import { useState } from "react";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { upsertTrainingSetAction } from "../actions/training.actions";
import { getOrderedExercises } from "../training-position";
import type { TrainingSession, TrainingSessionExercise, TrainingSet, TrainingSetInput } from "../types/training.types";

export function FreeTrainingView({ session, onMissing, onSaved, onUnauthenticated }: {
  session: TrainingSession;
  onMissing: () => void;
  onSaved: (exerciseId: string, set: TrainingSet) => void;
  onUnauthenticated: () => void;
}) {
  const [extraCounts, setExtraCounts] = useState<Record<string, number>>({});

  return (
    <>
      <header className="mt-5">
        <h2 className="font-brand text-lg font-bold text-text-primary">Carga libre</h2>
        <p className="mt-1 text-sm text-text-secondary">Registrá tus series a tu ritmo.</p>
      </header>
      <div className="mt-5 space-y-5">
        {getOrderedExercises(session).map((exercise) => {
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
                  key={`${exercise.id}-${number}-${exercise.series.find((set) => set.numeroSerie === number)?.fechaHoraRegistro ?? "draft"}`}
                  number={number}
                  onMissing={onMissing}
                  onSaved={(set) => onSaved(exercise.id, set)}
                  onUnauthenticated={onUnauthenticated}
                  saved={exercise.series.find((set) => set.numeroSerie === number)}
                  sessionId={session.id}
                />
              ))}
            </div>
            <button className="mt-3 min-h-11 text-sm font-semibold text-primary focus-visible:outline-primary" onClick={() => setExtraCounts((current) => ({ ...current, [exercise.id]: Math.max(current[exercise.id] ?? 0, highestSaved - exercise.seriesObjetivo) + 1 }))} type="button">+ Agregar serie</button>
          </section>;
        })}
      </div>
    </>
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
