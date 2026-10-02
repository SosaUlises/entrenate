"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ChevronDown, ChevronRight, Plus, Trash2, X } from "lucide-react";
import { useRef, useState, type FormEvent } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
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
  const [expandedDayId, setExpandedDayId] = useState<string | null>(() => dias[0]?.id ?? null);
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

  const handleAddDay = () => {
    setExpandedDayId(addDay());
  };

  const handleRemoveDay = (dayId: string) => {
    if (expandedDayId === dayId) {
      const index = dias.findIndex((day) => day.id === dayId);
      setExpandedDayId(dias[index + 1]?.id ?? dias[index - 1]?.id ?? null);
    }
    removeDay(dayId);
  };

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

      <form className="mt-8" onSubmit={(event) => void handleSubmit(event)}>
        <div>
          <label className="text-sm font-semibold text-text-primary" htmlFor="routine-name">
            Nombre de la rutina
          </label>
          <input
            className="mt-2 min-h-12 w-full rounded-control-sm border border-border/70 bg-surface/65 px-4 text-sm text-text-primary outline-none transition-[background-color,border-color,box-shadow] placeholder:text-text-secondary/55 hover:border-border-strong focus:border-primary focus:ring-2 focus:ring-primary/25 focus-visible:outline-primary"
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

        <div className="mt-8">
          <h2 className="font-brand text-lg font-bold text-text-primary">Días de entrenamiento</h2>
          <div className="mt-3 border-t border-border/60">
            {dias.map((day) => (
              <RoutineDaySection
                canRemove={dias.length > 1}
                day={day}
                expanded={expandedDayId === day.id}
                key={day.id}
                onRemove={handleRemoveDay}
                onRemoveExercise={removeDayExercise}
                onRename={renameDay}
                onEditExercise={(exerciseId) => setEditing({ dayId: day.id, exerciseId })}
                onToggle={() => setExpandedDayId((current) => current === day.id ? null : day.id)}
                basePath={basePath}
              />
            ))}
          </div>
          <button
            className="mt-5 inline-flex min-h-11 items-center gap-2 rounded-control-sm px-1 text-sm font-medium text-text-secondary transition-colors hover:text-primary focus-visible:outline-primary"
            onClick={handleAddDay}
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
        <Button className={`mt-7 min-h-[3.25rem] w-full rounded-control-sm text-[0.9375rem] font-bold sm:max-w-sm ${!canSubmit ? "disabled:border disabled:border-border/60 disabled:bg-surface/70 disabled:text-text-secondary disabled:opacity-100" : ""}`} disabled={!canSubmit || isSubmitting} isLoading={isSubmitting} type="submit">
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

function RoutineDaySection({
  day, canRemove, expanded, onRename, onRemove, onRemoveExercise, onEditExercise, onToggle, basePath,
}: {
  day: RoutineDraftDay;
  canRemove: boolean;
  expanded: boolean;
  onRename: (dayId: string, nombre: string) => void;
  onRemove: (dayId: string) => void;
  onRemoveExercise: (dayId: string, exerciseId: string) => void;
  onEditExercise: (exerciseId: string) => void;
  onToggle: () => void;
  basePath: string;
}) {
  const contentId = `routine-day-content-${day.id}`;

  return (
    <section aria-label={`Día de entrenamiento ${day.orden}`} className="min-w-0 border-b border-border/60">
      <div className="flex min-w-0 items-center gap-1">
        <button
          aria-controls={contentId}
          aria-expanded={expanded}
          className="flex min-h-14 min-w-0 flex-1 items-center gap-3 py-2 text-left focus-visible:outline-primary"
          onClick={onToggle}
          type="button"
        >
          <span className="min-w-0 flex-1 truncate font-brand text-base font-bold text-text-primary">{day.nombre}</span>
          <span className="shrink-0 text-xs font-medium text-info/80">
            {day.exercises.length} {day.exercises.length === 1 ? "ejercicio" : "ejercicios"}
          </span>
          <ChevronDown aria-hidden="true" className={`shrink-0 text-text-secondary/75 transition-transform motion-reduce:transition-none ${expanded ? "rotate-180" : ""}`} size={16} strokeWidth={1.75} />
        </button>
        {canRemove ? (
          <button
            aria-label={`Eliminar día ${day.orden}: ${day.nombre}`}
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:text-error focus-visible:outline-primary"
            onClick={() => onRemove(day.id)}
            type="button"
          >
            <Trash2 aria-hidden="true" size={17} />
          </button>
        ) : null}
      </div>

      <div className="pb-5" hidden={!expanded} id={contentId}>
        <label className="sr-only" htmlFor={`routine-day-${day.id}`}>Nombre del día {day.orden}</label>
        <input
          className="min-h-11 w-full rounded-control-sm border border-border/50 bg-surface/40 px-3 text-sm font-semibold text-text-primary outline-none transition-[background-color,border-color,box-shadow] hover:border-border-strong hover:bg-surface/55 focus:border-primary focus:bg-surface/55 focus:ring-2 focus:ring-primary/20 focus-visible:outline-primary"
          id={`routine-day-${day.id}`}
          maxLength={100}
          onChange={(event) => onRename(day.id, event.target.value)}
          required
          type="text"
          value={day.nombre}
        />

        {day.exercises.length === 0 ? (
          <p className="mt-4 text-sm leading-6 text-text-secondary">Todavía no agregaste ejercicios.</p>
        ) : (
          <ul className="mt-4 divide-y divide-border/50 border-y border-border/50">
            {day.exercises.map((exercise) => {
              const imageSrc = getExerciseImage(exercise.nombre);
              return (
                <li className="flex min-w-0 items-center gap-1 py-1" key={exercise.exerciseId}>
                  <button
                    aria-label={`Configurar ${exercise.nombre} en ${day.nombre}`}
                    className="flex min-h-16 min-w-0 flex-1 items-center gap-3 rounded-control-sm py-2 text-left focus-visible:outline-primary"
                    onClick={() => onEditExercise(exercise.exerciseId)}
                    type="button"
                  >
                    {imageSrc ? (
                      <Image alt="" className="size-12 shrink-0 rounded-control-sm object-cover" height={48} src={imageSrc} width={48} />
                    ) : null}
                    <span className="min-w-0 flex-1">
                      <span className="block break-words font-brand text-sm font-bold leading-5 text-text-primary">{exercise.nombre}</span>
                      <span className="mt-1 block text-xs leading-5 text-text-secondary">
                        {exercise.cantidadSeries}×{exercise.repeticionesMinimas}–{exercise.repeticionesMaximas} · RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoSegundos} s
                      </span>
                    </span>
                    <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary" size={17} />
                  </button>
                  <button
                    aria-label={`Quitar ${exercise.nombre} de ${day.nombre}`}
                    className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:text-error focus-visible:outline-primary"
                    onClick={() => onRemoveExercise(day.id, exercise.exerciseId)}
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
      </div>
    </section>
  );
}

function AddExercisesLink({ basePath, dayId }: { basePath: string; dayId: string }) {
  return (
    <Link
      className="mt-2 inline-flex min-h-11 items-center gap-2 rounded-control-sm bg-primary/5 px-2 text-sm font-semibold text-primary transition-colors hover:bg-primary/10 hover:text-text-primary focus-visible:outline-primary"
      href={`${basePath}/exercises?day=${encodeURIComponent(dayId)}`}
    >
      <Plus aria-hidden="true" size={17} />
      Agregar ejercicios
    </Link>
  );
}
