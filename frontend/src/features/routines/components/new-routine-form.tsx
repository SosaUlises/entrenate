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
import { deleteRoutineAction } from "../actions/delete-routine.action";
import { updateRoutineAction } from "../actions/update-routine.action";
import { mapRoutineDraftToRequest } from "../map-routine-draft";
import { RoutineExerciseEditor } from "./routine-exercise-editor";
import { useRoutineDraft, type RoutineDraftDay } from "../context/routine-draft-context";
import { useRoutineDraftPersistence } from "../context/routine-draft-persistence";

export function NewRoutineForm() {
  return <RoutineForm />;
}

export function RoutineForm({ routineId }: { routineId?: string }) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const { clearPersistedDraft } = useRoutineDraftPersistence();
  const { nombre, setNombre, descripcion, dias, addDay, renameDay, removeDay, removeDayExercise, updateDayExercise, resetDraft } = useRoutineDraft();
  const [editing, setEditing] = useState<{ dayId: string; exerciseId: string } | null>(null);
  const [expandedDayId, setExpandedDayId] = useState<string | null>(() => routineId ? null : (dias[0]?.id ?? null));
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [deleteState, setDeleteState] = useState<"idle" | "deleting" | "conflict" | "error">("idle");
  const [notFound, setNotFound] = useState(false);
  const [submitError, setSubmitError] = useState<{ message: string; details?: string[] } | null>(null);
  const submittingRef = useRef(false);
  const deletingRef = useRef(false);
  const deleteDialogRef = useRef<HTMLDialogElement>(null);
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
        if (!routineId) clearPersistedDraft();
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

  const handleDeleteRoutine = async () => {
    if (!routineId || deletingRef.current) return;

    deletingRef.current = true;
    setDeleteState("deleting");

    try {
      const result = await deleteRoutineAction(routineId);
      if (result.status === "deleted") {
        resetDraft();
        router.replace("/routines");
        router.refresh();
        return;
      }
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      if (result.status === "not-found") {
        deleteDialogRef.current?.close();
        setNotFound(true);
        return;
      }
      setDeleteState(result.status);
    } catch {
      setDeleteState("error");
    } finally {
      deletingRef.current = false;
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
            className="mt-5 inline-flex min-h-11 items-center gap-2 rounded-control-sm px-1 text-sm font-medium text-text-secondary transition-colors hover:text-text-primary focus-visible:outline-primary"
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
      {routineId ? (
        <div className="mt-10 border-t border-border/55 pt-6">
          <button
            className="inline-flex min-h-11 items-center gap-2 rounded-control-sm px-1 text-sm font-semibold text-error transition-colors hover:text-text-primary focus-visible:outline-error"
            onClick={() => {
              setDeleteState("idle");
              deleteDialogRef.current?.showModal();
            }}
            type="button"
          >
            <Trash2 aria-hidden="true" size={17} />
            Eliminar rutina
          </button>
        </div>
      ) : null}
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
      {routineId ? (
        <dialog
          aria-describedby="delete-routine-description"
          aria-labelledby="delete-routine-title"
          aria-modal="true"
          className="mt-auto w-full max-w-full rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
          onCancel={(event) => {
            if (deleteState === "deleting") event.preventDefault();
          }}
          onClick={(event) => {
            if (event.target === event.currentTarget && deleteState !== "deleting") {
              event.currentTarget.close();
            }
          }}
          onClose={() => {
            if (deleteState !== "deleting") setDeleteState("idle");
          }}
          ref={deleteDialogRef}
        >
          <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
          <div className="px-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] pt-5 sm:p-6">
            {deleteState === "conflict" ? (
              <>
                <h2 className="font-brand text-xl font-bold text-text-primary" id="delete-routine-title">No se puede eliminar</h2>
                <p className="mt-3 text-sm leading-6 text-text-secondary" id="delete-routine-description">
                  No podés eliminar esta rutina mientras tenés un entrenamiento en curso.
                </p>
                <Button autoFocus className="mt-6" onClick={() => deleteDialogRef.current?.close()} variant="secondary">
                  Volver
                </Button>
              </>
            ) : (
              <>
                <h2 className="font-brand text-xl font-bold text-text-primary" id="delete-routine-title">¿Eliminar esta rutina?</h2>
                <p className="mt-3 text-sm leading-6 text-text-secondary" id="delete-routine-description">
                  Se eliminará la rutina y su configuración. Tus entrenamientos anteriores seguirán disponibles en Historial.
                </p>
                {deleteState === "error" ? (
                  <p className="mt-4 text-sm leading-5 text-error" role="alert">
                    No pudimos eliminar la rutina. Intentá nuevamente.
                  </p>
                ) : null}
                <div className="mt-6 flex flex-wrap justify-end gap-3">
                  <Button autoFocus disabled={deleteState === "deleting"} onClick={() => deleteDialogRef.current?.close()} variant="secondary">
                    Cancelar
                  </Button>
                  <button
                    className="inline-flex min-h-12 items-center justify-center gap-2 rounded-control bg-error px-5 text-sm font-semibold text-background transition-opacity hover:opacity-90 focus-visible:outline-error disabled:cursor-not-allowed disabled:opacity-60"
                    disabled={deleteState === "deleting"}
                    onClick={() => void handleDeleteRoutine()}
                    type="button"
                  >
                    <Trash2 aria-hidden="true" size={17} />
                    {deleteState === "deleting" ? "Eliminando..." : "Eliminar rutina"}
                  </button>
                </div>
              </>
            )}
          </div>
        </dialog>
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
          className="group flex min-h-14 min-w-0 flex-1 items-center gap-3 py-2 text-left focus-visible:outline-primary"
          onClick={onToggle}
          type="button"
        >
          <span className="min-w-0 flex-1 truncate font-brand text-base font-bold text-text-primary">{day.nombre}</span>
          <span className="shrink-0 text-xs font-medium text-info/80">
            {day.exercises.length} {day.exercises.length === 1 ? "ejercicio" : "ejercicios"}
          </span>
          <ChevronDown aria-hidden="true" className={`shrink-0 text-text-secondary/70 transition-[color,transform] group-hover:text-text-primary motion-reduce:transition-none ${expanded ? "rotate-180" : ""}`} size={16} strokeWidth={1.75} />
        </button>
        {canRemove ? (
          <button
            aria-label={`Eliminar día ${day.orden}: ${day.nombre}`}
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary/65 transition-colors hover:bg-error/10 hover:text-error focus-visible:text-error focus-visible:outline-primary"
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
          <ul className="mt-4 divide-y divide-border/45 border-y border-border/45">
            {day.exercises.map((exercise) => {
              const imageSrc = getExerciseImage(exercise.nombre);
              return (
                <li className="flex min-w-0 items-center gap-1 py-1.5" key={exercise.exerciseId}>
                  <button
                    aria-label={`Configurar ${exercise.nombre} en ${day.nombre}`}
                    className="group flex min-h-16 min-w-0 flex-1 items-center gap-3 rounded-control-sm py-2 text-left transition-colors focus-visible:outline-primary"
                    onClick={() => onEditExercise(exercise.exerciseId)}
                    type="button"
                  >
                    {imageSrc ? (
                      <Image alt="" className="size-12 shrink-0 rounded-control-sm object-cover" height={48} src={imageSrc} width={48} />
                    ) : null}
                    <span className="min-w-0 flex-1">
                      <span className="block break-words font-brand text-base font-bold leading-5 text-text-primary">{exercise.nombre}</span>
                      <span className="mt-1 block break-words text-sm leading-5 text-text-secondary">
                        {exercise.cantidadSeries}×{exercise.repeticionesMinimas}–{exercise.repeticionesMaximas} · RIR {exercise.rirObjetivoMinimo}–{exercise.rirObjetivoMaximo} · {exercise.descansoSegundos} s
                      </span>
                    </span>
                    <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary/65 transition-colors group-hover:text-primary" size={17} />
                  </button>
                  <button
                    aria-label={`Quitar ${exercise.nombre} de ${day.nombre}`}
                    className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary/60 transition-colors hover:bg-error/10 hover:text-error focus-visible:text-error focus-visible:outline-primary"
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
      className="mt-3 inline-flex min-h-11 items-center gap-2 rounded-control-sm bg-primary/8 px-3 text-sm font-semibold text-primary transition-colors hover:bg-primary/12 hover:text-text-primary focus-visible:outline-primary"
      href={`${basePath}/exercises?day=${encodeURIComponent(dayId)}`}
    >
      <Plus aria-hidden="true" size={17} />
      Agregar ejercicios
    </Link>
  );
}
