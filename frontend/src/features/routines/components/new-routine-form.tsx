"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ChevronDown, ChevronRight, Plus, Trash2 } from "lucide-react";
import { useRef, useState, type FormEvent } from "react";
import { Alert } from "@/components/ui/alert";
import { Button, buttonClassName } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { createRoutineAction } from "../actions/create-routine.action";
import { deleteRoutineAction } from "../actions/delete-routine.action";
import { updateRoutineAction } from "../actions/update-routine.action";
import { mapRoutineDraftToRequest } from "../map-routine-draft";
import { RoutineExerciseEditor } from "./routine-exercise-editor";
import { RoutineExerciseTarget } from "./routine-exercise-target";
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
      <section className="mx-auto w-full max-w-xl pt-4">
        <p className="text-sm text-text-primary">No encontramos esta rutina.</p>
        <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-text-primary focus-visible:outline-2 focus-visible:outline-focus" href="/routines">
          Volver a mis rutinas
        </Link>
      </section>
    );
  }

  return (
    <section aria-labelledby="new-routine-title" className="mx-auto w-full max-w-xl pb-24">
      <div className="flex items-center gap-1">
        <Link
          aria-label={routineId ? "Volver al detalle de la rutina" : "Volver a mis rutinas"}
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
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
          <label className="text-[0.8125rem] font-semibold tracking-[0.04em] text-text-secondary" htmlFor="routine-name">
            Nombre de la rutina
          </label>
          <input
            className="mt-1 min-h-14 w-full border-0 border-b border-border-strong bg-transparent px-0 font-brand text-xl font-bold text-text-primary caret-thermal-accent outline-none transition-colors placeholder:font-sans placeholder:text-base placeholder:font-normal placeholder:text-text-muted hover:border-text-muted focus:border-focus focus-visible:outline-none"
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

        <div className={routineId ? "mt-10" : "mt-8"}>
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
            className="mt-5 inline-flex min-h-11 items-center gap-2 rounded-control-sm px-2 py-2 text-sm font-semibold text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus [&_svg]:text-thermal-accent"
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
        <div className="fixed inset-x-0 bottom-[calc(4.75rem+env(safe-area-inset-bottom))] z-20 border-t border-border/70 bg-background/95 px-4 py-3 backdrop-blur-md">
          <div className="mx-auto flex w-full max-w-xl justify-end">
            <Button className="sm:max-w-xs" disabled={!canSubmit || isSubmitting} fullWidth isLoading={isSubmitting} size="md" type="submit">
              {isSubmitting ? (routineId ? "Guardando..." : "Creando rutina...") : (routineId ? "Guardar cambios" : "Crear rutina")}
            </Button>
          </div>
        </div>
      </form>
      {routineId ? (
        <div className="mt-10 border-t border-border/55 pt-6">
          <button
            className="inline-flex min-h-11 items-center gap-2 rounded-control-sm px-2 text-sm font-semibold text-text-secondary transition-colors hover:bg-error/10 hover:text-error focus-visible:outline-2 focus-visible:outline-error [&_svg]:text-error"
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
                  <Button
                    isLoading={deleteState === "deleting"}
                    onClick={() => void handleDeleteRoutine()}
                    variant="destructive"
                  >
                    <Trash2 aria-hidden="true" size={17} />
                    {deleteState === "deleting" ? "Eliminando..." : "Eliminar rutina"}
                  </Button>
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
      <div className="flex min-w-0 items-center">
        <button
          aria-controls={contentId}
          aria-expanded={expanded}
          className={`group flex min-h-14 min-w-0 flex-1 items-center gap-3 rounded-control-sm px-2 py-2 text-left transition-colors focus-visible:outline-2 focus-visible:outline-focus ${expanded ? "bg-surface-active/35" : "hover:bg-surface-interactive/45"}`}
          onClick={onToggle}
          type="button"
        >
          <span className="min-w-0 flex-1 truncate font-brand text-base font-bold text-text-primary">Día {day.orden}</span>
          <span className="shrink-0 text-[0.8125rem] font-semibold text-info">
            {day.exercises.length} {day.exercises.length === 1 ? "ejercicio" : "ejercicios"}
          </span>
          <ChevronDown aria-hidden="true" className={`shrink-0 transition-[color,transform] motion-reduce:transition-none ${expanded ? "rotate-180 text-thermal-accent" : "text-text-muted group-hover:text-text-primary"}`} size={16} strokeWidth={1.75} />
        </button>
      </div>

      <div className="pb-5" hidden={!expanded} id={contentId}>
        <label className="text-[0.8125rem] font-semibold tracking-[0.04em] text-text-secondary" htmlFor={`routine-day-${day.id}`}>
          Nombre del día<span className="sr-only"> {day.orden}</span>
        </label>
        <input
          className="mt-1 min-h-12 w-full border-0 border-b border-border bg-transparent px-0 font-brand text-base font-bold text-text-primary caret-thermal-accent outline-none transition-colors hover:border-border-strong focus:border-focus focus-visible:outline-none"
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
            {day.exercises.map((exercise) => (
              <li className="flex min-w-0 items-center gap-1 py-1.5" key={exercise.exerciseId}>
                  <button
                    aria-label={`Configurar ${exercise.nombre} en ${day.nombre}`}
                    className="group flex min-h-[4.5rem] min-w-0 flex-1 items-center gap-3 rounded-control-sm px-1 py-2 text-left transition-colors hover:bg-surface-interactive/40 focus-visible:outline-2 focus-visible:outline-focus"
                    onClick={() => onEditExercise(exercise.exerciseId)}
                    type="button"
                  >
                    <span className="min-w-0 flex-1">
                      <span className="block break-words font-brand text-base font-bold leading-5 text-text-primary">{exercise.nombre}</span>
                      <RoutineExerciseTarget className="mt-1.5" {...exercise} />
                    </span>
                    <ChevronRight aria-hidden="true" className="shrink-0 text-text-muted transition-colors group-hover:text-text-primary" size={17} />
                  </button>
                  <button
                    aria-label={`Quitar ${exercise.nombre} de ${day.nombre}`}
                    className="flex min-h-11 shrink-0 items-center rounded-control-sm px-2 text-[0.8125rem] font-semibold text-text-secondary transition-colors hover:bg-error/10 hover:text-error focus-visible:text-error focus-visible:outline-2 focus-visible:outline-error"
                    onClick={() => onRemoveExercise(day.id, exercise.exerciseId)}
                    type="button"
                  >
                    Quitar
                  </button>
              </li>
            ))}
          </ul>
        )}
        <div className={`${day.exercises.length === 0 ? "mt-4" : "mt-3"} flex flex-wrap items-center justify-between gap-2`}>
          <AddExercisesLink basePath={basePath} dayId={day.id} />
          {canRemove ? (
            <button
              className="inline-flex min-h-11 items-center gap-2 rounded-control-sm px-2 text-[0.8125rem] font-semibold text-text-secondary transition-colors hover:bg-error/10 hover:text-error focus-visible:text-error focus-visible:outline-error"
              onClick={() => onRemove(day.id)}
              type="button"
            >
              <Trash2 aria-hidden="true" size={16} />
              Eliminar día
            </button>
          ) : null}
        </div>
      </div>
    </section>
  );
}

function AddExercisesLink({ basePath, dayId }: { basePath: string; dayId: string }) {
  return (
    <Link
      className={buttonClassName({ className: "[&_svg]:text-thermal-accent", size: "sm", variant: "secondary" })}
      href={`${basePath}/exercises?day=${encodeURIComponent(dayId)}`}
    >
      <Plus aria-hidden="true" size={17} />
      Agregar ejercicios
    </Link>
  );
}
