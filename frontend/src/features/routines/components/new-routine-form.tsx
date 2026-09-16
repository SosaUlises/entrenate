"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ChevronRight, Plus, Trash2, X } from "lucide-react";
import { useRef, useState, type FormEvent } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { createRoutineAction } from "../actions/create-routine.action";
import { updateRoutineAction } from "../actions/update-routine.action";
import { mapRoutineDraftToRequest } from "../map-routine-draft";
import { RoutineExerciseEditor } from "./routine-exercise-editor";
import { useRoutineDraft, type RoutineDraftDay } from "../context/routine-draft-context";

export function NewRoutineForm() {
  return <RoutineForm />;
}

export function RoutineForm({ routineId }: { routineId?: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const { nombre, setNombre, descripcion, dias, addDay, renameDay, removeDay, removeDayExercise, updateDayExercise, resetDraft } = useRoutineDraft();
  const [editing, setEditing] = useState<{ dayId: string; exerciseId: string } | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notFound, setNotFound] = useState(false);
  const [submitError, setSubmitError] = useState<{ message: string; details?: string[] } | null>(null);
  const submittingRef = useRef(false);
  const editingExercise = editing
    ? dias.find((day) => day.id === editing.dayId)?.exercises.find((exercise) => exercise.exerciseId === editing.exerciseId)
    : undefined;
  const canSubmit = mapRoutineDraftToRequest({ nombre, descripcion, dias }) !== null;
  const basePath = routineId ? `/routines/${routineId}/edit` : "/routines/new";
  const backHref = routineId ? `/routines/${routineId}` : "/routines";

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submittingRef.current) return;

    const request = mapRoutineDraftToRequest({ nombre, descripcion, dias });
    if (!request) {
      setSubmitError({ message: routineId ? "Revisá los datos antes de guardar." : "Revisá los datos de la rutina antes de crearla." });
      return;
    }

    submittingRef.current = true;
    setIsSubmitting(true);
    setSubmitError(null);
    let succeeded = false;

    try {
      const result = routineId
        ? await updateRoutineAction(routineId, request)
        : await createRoutineAction(request);
      if (result.status === "created" || result.status === "updated") {
        succeeded = true;
        resetDraft();
        router.replace(routineId ? `/routines/${routineId}` : "/routines");
        return;
      }
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      if (result.status === "validation-error") {
        setSubmitError({ message: routineId ? "No pudimos guardar los cambios." : "No pudimos crear la rutina.", details: result.messages });
        return;
      }
      if (result.status === "not-found") {
        setNotFound(true);
        return;
      }
      setSubmitError({ message: routineId ? "No pudimos guardar los cambios. Intentá nuevamente." : "No pudimos crear la rutina. Intentá nuevamente." });
    } catch {
      setSubmitError({ message: routineId ? "No pudimos guardar los cambios. Intentá nuevamente." : "No pudimos crear la rutina. Intentá nuevamente." });
    } finally {
      if (!succeeded) {
        submittingRef.current = false;
        setIsSubmitting(false);
      }
    }
  };

  if (notFound) {
    return (
      <section className="mx-auto w-full max-w-xl rounded-card border border-border bg-surface p-5">
        <p className="text-sm text-text-primary">No encontramos esta rutina.</p>
        <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href="/routines">
          Volver a mis rutinas
        </Link>
      </section>
    );
  }

  return (
    <section aria-labelledby="new-routine-title" className="mx-auto w-full max-w-xl">
      <div className="flex items-center gap-1">
        <Link
          aria-label={routineId ? "Volver al detalle de la rutina" : "Volver a mis rutinas"}
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary"
          href={backHref}
        >
          <ArrowLeft aria-hidden="true" size={20} />
        </Link>
        <h1 className="font-brand text-xl font-bold text-text-primary" id="new-routine-title">
          {routineId ? "Editar rutina" : "Nueva rutina"}
        </h1>
      </div>
      <p className="mt-1 pl-12 text-sm leading-6 text-text-secondary">
        {routineId ? "Modificá la estructura y configuración de tu rutina." : "Armá una rutina adaptada a tu forma de entrenar."}
      </p>

      <form className="mt-9" onSubmit={(event) => void handleSubmit(event)}>
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
                basePath={basePath}
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

        {submitError ? (
          <Alert className="mt-6" title={submitError.message} variant="error">
            {submitError.details?.length ? (
              <ul className="list-disc pl-5">
                {submitError.details.map((message, index) => <li key={`${index}-${message}`}>{message}</li>)}
              </ul>
            ) : null}
          </Alert>
        ) : null}
        <Button className="mt-10 w-full sm:w-auto" disabled={!canSubmit || isSubmitting} isLoading={isSubmitting} type="submit">
          {isSubmitting ? (routineId ? "Guardando..." : "Creando rutina...") : (routineId ? "Guardar cambios" : "Crear rutina")}
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
  day, canRemove, onRename, onRemove, onRemoveExercise, onEditExercise, basePath,
}: {
  day: RoutineDraftDay;
  canRemove: boolean;
  onRename: (dayId: string, nombre: string) => void;
  onRemove: (dayId: string) => void;
  onRemoveExercise: (dayId: string, exerciseId: string) => void;
  onEditExercise: (exerciseId: string) => void;
  basePath: string;
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
      <AddExercisesLink basePath={basePath} dayId={day.id} />
    </section>
  );
}

function AddExercisesLink({ basePath, dayId }: { basePath: string; dayId: string }) {
  return (
    <Link
      className="mt-3 inline-flex min-h-11 items-center gap-2 rounded-control-sm border border-primary/30 bg-primary/10 px-4 text-sm font-semibold text-primary hover:border-primary/60 hover:bg-primary/15 focus-visible:outline-primary"
      href={`${basePath}/exercises?day=${encodeURIComponent(dayId)}`}
    >
      <Plus aria-hidden="true" size={17} />
      Agregar ejercicios
    </Link>
  );
}
