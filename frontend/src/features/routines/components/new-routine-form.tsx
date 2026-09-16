"use client";

import Image from "next/image";
import Link from "next/link";
import { ArrowLeft, ChevronRight, Plus, Trash2, X } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { RoutineExerciseEditor } from "./routine-exercise-editor";
import { useRoutineDraft, type RoutineDraftDay } from "../context/routine-draft-context";
import { validateRoutineExerciseConfig } from "../types/routine-draft-exercise";

export function NewRoutineForm() {
  const { nombre, setNombre, dias, addDay, renameDay, removeDay, removeDayExercise, updateDayExercise } = useRoutineDraft();
  const [editing, setEditing] = useState<{ dayId: string; exerciseId: string } | null>(null);
  const editingExercise = editing
    ? dias.find((day) => day.id === editing.dayId)?.exercises.find((exercise) => exercise.exerciseId === editing.exerciseId)
    : undefined;
  const canCreate = nombre.trim().length > 0 && nombre.length <= 100 &&
    dias.length > 0 && dias.every((day) => day.nombre.trim().length > 0 && day.nombre.length <= 100) &&
    dias.some((day) => day.exercises.length > 0) &&
    dias.every((day) => day.exercises.every((exercise) =>
      Object.keys(validateRoutineExerciseConfig(exercise)).length === 0));

  return (
    <section aria-labelledby="new-routine-title" className="mx-auto w-full max-w-xl">
      <div className="flex items-center gap-1">
        <Link
          aria-label="Volver a mis rutinas"
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary"
          href="/routines"
        >
          <ArrowLeft aria-hidden="true" size={20} />
        </Link>
        <h1 className="font-brand text-xl font-bold text-text-primary" id="new-routine-title">
          Nueva rutina
        </h1>
      </div>
      <p className="mt-1 pl-12 text-sm leading-6 text-text-secondary">
        Armá una rutina adaptada a tu forma de entrenar.
      </p>

      <form className="mt-9" onSubmit={(event) => event.preventDefault()}>
        <div>
          <label className="text-sm font-semibold text-text-primary" htmlFor="routine-name">
            Nombre de la rutina
          </label>
          <Input
            className="mt-3"
            id="routine-name"
            maxLength={100}
            name="nombre"
            onChange={(event) => setNombre(event.target.value)}
            placeholder="Ej. Push, Piernas, Full Body..."
            required
            type="text"
            value={nombre}
          />
        </div>

        <div className="mt-9">
          <h2 className="font-brand text-lg font-bold text-text-primary">Días de entrenamiento</h2>
          <div className="mt-3 space-y-3">
            {dias.map((day) => (
              <RoutineDayCard
                canRemove={dias.length > 1}
                day={day}
                key={day.id}
                onRemove={removeDay}
                onRemoveExercise={removeDayExercise}
                onRename={renameDay}
                onEditExercise={(exerciseId) => setEditing({ dayId: day.id, exerciseId })}
              />
            ))}
          </div>
          <button
            className="mt-4 inline-flex min-h-11 items-center gap-2 rounded-control-sm px-2 text-sm font-semibold text-primary hover:bg-primary/10 focus-visible:outline-primary"
            onClick={addDay}
            type="button"
          >
            <Plus aria-hidden="true" size={17} />
            Agregar día
          </button>
        </div>

        <Button className="mt-10 w-full sm:w-auto" disabled={!canCreate} type="button">
          Crear rutina
        </Button>
      </form>
      {editing && editingExercise ? (
        <RoutineExerciseEditor
          exercise={editingExercise}
          key={`${editing.dayId}:${editing.exerciseId}`}
          onClose={() => setEditing(null)}
          onSave={(config) => {
            updateDayExercise(editing.dayId, editing.exerciseId, config);
            setEditing(null);
          }}
        />
      ) : null}
    </section>
  );
}

function RoutineDayCard({
  day, canRemove, onRename, onRemove, onRemoveExercise, onEditExercise,
}: {
  day: RoutineDraftDay;
  canRemove: boolean;
  onRename: (dayId: string, nombre: string) => void;
  onRemove: (dayId: string) => void;
  onRemoveExercise: (dayId: string, exerciseId: string) => void;
  onEditExercise: (exerciseId: string) => void;
}) {
  return (
    <section aria-label={`Día de entrenamiento ${day.orden}`} className="rounded-card border border-border/60 bg-surface/45 p-4">
      <div className="flex min-w-0 items-center gap-2">
        <label className="sr-only" htmlFor={`routine-day-${day.id}`}>Nombre del día {day.orden}</label>
        <input
          className="min-w-0 flex-1 rounded-control-sm border border-transparent bg-transparent px-2 py-2 font-brand text-base font-bold text-text-primary outline-none hover:border-border focus:border-primary focus-visible:outline-primary"
          id={`routine-day-${day.id}`}
          maxLength={100}
          onChange={(event) => onRename(day.id, event.target.value)}
          required
          type="text"
          value={day.nombre}
        />
        {canRemove ? (
          <button
            aria-label={`Eliminar día ${day.orden}: ${day.nombre}`}
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary hover:bg-surface-elevated hover:text-text-primary focus-visible:outline-primary"
            onClick={() => onRemove(day.id)}
            type="button"
          >
            <Trash2 aria-hidden="true" size={17} />
          </button>
        ) : null}
      </div>

      {day.exercises.length === 0 ? (
        <p className="mt-2 px-2 text-sm text-text-secondary">Todavía no agregaste ejercicios.</p>
      ) : (
        <ul className="mt-2 divide-y divide-border/50">
          {day.exercises.map((exercise) => {
            const imageSrc = getExerciseImage(exercise.nombre);
            return (
              <li
                className="flex min-w-0 items-center gap-1 py-1"
                key={exercise.exerciseId}
                onClick={() => onEditExercise(exercise.exerciseId)}
              >
                <button
                  aria-label={`Configurar ${exercise.nombre} en ${day.nombre}`}
                  className="flex min-h-14 min-w-0 flex-1 items-center gap-3 rounded-control-sm p-1 text-left hover:bg-surface-elevated/60 focus-visible:outline-primary"
                  type="button"
                >
                  {imageSrc ? (
                    <Image alt="" className="size-12 shrink-0 rounded-control-sm object-cover" height={48} src={imageSrc} width={48} />
                  ) : null}
                  <div className="min-w-0 flex-1">
                    <p className="font-brand text-sm font-bold text-text-primary">{exercise.nombre}</p>
                    <p className="mt-1 text-xs text-text-secondary">{exercise.cantidadSeries} series · {exercise.repeticionesMinimas}–{exercise.repeticionesMaximas} reps</p>
                    <p className="mt-0.5 text-xs text-text-secondary">RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoSegundos} s</p>
                  </div>
                  <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary" size={17} />
                </button>
                <button
                  aria-label={`Quitar ${exercise.nombre} de ${day.nombre}`}
                  className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary hover:bg-surface-elevated hover:text-text-primary focus-visible:outline-primary"
                  onClick={(event) => {
                    event.stopPropagation();
                    onRemoveExercise(day.id, exercise.exerciseId);
                  }}
                  type="button"
                >
                  <X aria-hidden="true" size={18} />
                </button>
              </li>
            );
          })}
        </ul>
      )}
      <AddExercisesLink dayId={day.id} />
    </section>
  );
}

function AddExercisesLink({ dayId }: { dayId: string }) {
  return (
    <Link
      className="mt-3 inline-flex min-h-11 items-center gap-2 rounded-control-sm border border-primary/30 bg-primary/10 px-4 text-sm font-semibold text-primary hover:border-primary/60 hover:bg-primary/15 focus-visible:outline-primary"
      href={`/routines/new/exercises?day=${encodeURIComponent(dayId)}`}
    >
      <Plus aria-hidden="true" size={17} />
      Agregar ejercicios
    </Link>
  );
}
