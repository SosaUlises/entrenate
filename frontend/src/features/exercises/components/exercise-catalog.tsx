"use client";

import Image from "next/image";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, Check, ChevronRight, Plus, Search, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getExercisesAction, type GetExercisesActionResult } from "../actions/get-exercises.action";
import {
  ALL_MUSCLE_FILTER_ID,
  getVisibleMuscleFilters,
  matchesMuscleFilter,
} from "../exercise-muscle-filters";
import type { Exercise } from "../types/exercise.types";
import { hasEntrenateExerciseDemo } from "./exercise-demo-registry";

const ExerciseMovementDemo = dynamic(() => import("./exercise-movement-demo"), { ssr: false });

type CatalogState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; exercises: Exercise[] };

function normalize(value: string): string {
  return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es-AR");
}

type ExerciseCatalogBaseProps = {
  backHref?: string;
  backLabel?: string;
  description?: string;
  title?: string;
};

type ExerciseCatalogProps = ExerciseCatalogBaseProps & (
  | { mode: "browse" }
  | {
      mode: "select";
      initialSelectedIds?: string[];
      onConfirmSelection: (exercises: Exercise[]) => void;
    }
);

export function ExerciseCatalog(props: ExerciseCatalogProps) {
  const {
    backHref = "/home",
    backLabel = "Volver a inicio",
    description = props.mode === "browse"
      ? "Explorá ejercicios, equipamiento y técnica."
      : "Elegí los ejercicios que querés agregar.",
    title = "Ejercicios",
  } = props;
  const initialSelectedIds = props.mode === "select" ? (props.initialSelectedIds ?? []) : [];
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<CatalogState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [selectedIds, setSelectedIds] = useState<string[]>(initialSelectedIds);
  const selectedIdSet = new Set(selectedIds);
  const selectedExercises = state.status === "ready"
    ? state.exercises.filter((exercise) => selectedIdSet.has(exercise.id))
    : [];

  useEffect(() => {
    let active = true;

    const handleResult = (result: GetExercisesActionResult) => {
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      setState(result);
    };

    void getExercisesAction().then(handleResult, () => {
      if (active) setState({ status: "error" });
    });

    return () => {
      active = false;
    };
  }, [invalidateSession, requestKey, router]);

  return (
    <section aria-labelledby="exercises-title" className="mx-auto w-full min-w-0 max-w-xl">
      {props.mode === "select" ? (
        <div className="flex items-center gap-1">
          <Link
            aria-label={backLabel}
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary"
            href={backHref}
          >
            <ArrowLeft aria-hidden="true" size={20} />
          </Link>
          <h1 className="font-brand text-xl font-bold text-text-primary" id="exercises-title">
            {title}
          </h1>
        </div>
      ) : (
        <h1 className="font-brand text-2xl font-bold text-text-primary" id="exercises-title">
          {title}
        </h1>
      )}
      <p className={`text-sm text-text-secondary ${props.mode === "select" ? "mt-1 pl-12" : "mt-2 leading-6"}`}>
        {description}
      </p>

      {state.status === "loading" ? (
        <ExerciseCatalogSkeleton mode={props.mode} />
      ) : state.status === "error" ? (
        <div className="mt-8">
          <p className="text-sm text-text-primary">No pudimos cargar los ejercicios.</p>
          <Button
            className="mt-5"
            onClick={() => {
              setState({ status: "loading" });
              setRequestKey((key) => key + 1);
            }}
            variant="secondary"
          >
            Reintentar
          </Button>
        </div>
      ) : (
        <ExerciseSelector
          exercises={state.exercises}
          initialSelectedIds={initialSelectedIds}
          mode={props.mode}
          onSelectionChange={props.mode === "select" ? setSelectedIds : undefined}
        />
      )}

      {props.mode === "select" && state.status === "ready" ? (
        <div className="fixed inset-x-0 bottom-0 z-10 border-t border-border/60 bg-background/95 backdrop-blur-md">
          <div className="mx-auto flex max-w-xl items-center justify-between gap-3 px-4 pt-3 pb-[calc(0.75rem+env(safe-area-inset-bottom))] sm:px-0">
            <p aria-live="polite" className="text-sm font-medium text-text-secondary">
              {selectedExercises.length === 1
                ? "1 seleccionado"
                : `${selectedExercises.length} seleccionados`}
            </p>
            <Button
              className="min-h-12 rounded-control-sm disabled:border disabled:border-border-strong disabled:bg-surface-elevated disabled:text-text-secondary disabled:opacity-100"
              disabled={selectedExercises.length === 0}
              onClick={() => props.onConfirmSelection(selectedExercises)}
            >
              {selectedExercises.length === 0
                ? "Agregar ejercicios"
                : selectedExercises.length === 1
                ? "Agregar 1 ejercicio"
                : `Agregar ${selectedExercises.length} ejercicios`}
            </Button>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function ExerciseCatalogSkeleton({ mode }: { mode: "browse" | "select" }) {
  const compact = mode === "select";

  return (
    <div aria-label="Cargando ejercicios" className="mt-4 animate-pulse" role="status">
      <div className="h-12 w-full rounded-control-sm bg-surface-elevated" />
      <div className="mt-3 flex gap-1.5 overflow-hidden">
        {[0, 1, 2, 3].map((item) => (
          <div className={`flex shrink-0 flex-col items-center ${compact ? "h-[5.25rem] min-w-[4.5rem]" : "h-[5.75rem] min-w-[4.75rem]"}`} key={item}>
            <div className={compact ? "size-12 rounded-control-sm bg-surface-elevated" : "size-14 rounded-control-sm bg-surface-elevated"} />
            <div className="mt-1 h-3 w-10 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
      <div className="mt-2 border-y border-border/50">
        {[0, 1, 2, 3].map((item) => (
          <div className="flex min-h-[4.5rem] items-center border-b border-border/50 py-3.5 last:border-b-0" key={item}>
            <div className="min-w-0 flex-1">
              <div className="h-4 w-3/4 rounded bg-surface-elevated" />
              <div className="mt-2 h-3 w-28 rounded bg-surface-elevated" />
            </div>
            <div className="size-5 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
    </div>
  );
}

type ExerciseSelectorProps = {
  exercises: Exercise[];
  initialSelectedIds?: string[];
  mode: "browse" | "select";
  onSelectionChange?: (selectedIds: string[]) => void;
};

export function ExerciseSelector({ exercises, initialSelectedIds = [], mode, onSelectionChange }: ExerciseSelectorProps) {
  const [query, setQuery] = useState("");
  const [muscleFilterId, setMuscleFilterId] = useState(ALL_MUSCLE_FILTER_ID);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set(initialSelectedIds));
  const [preview, setPreview] = useState<Exercise | null>(null);
  const dialogRef = useRef<HTMLDialogElement>(null);

  const muscleFilters = useMemo(
    () => getVisibleMuscleFilters(exercises.map((exercise) => exercise.grupoMuscularPrincipal)),
    [exercises],
  );
  const selectedMuscleFilter =
    muscleFilters.find((filter) => filter.id === muscleFilterId) ?? muscleFilters[0];

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (preview && !dialog.open) {
      dialog.showModal();
    } else if (!preview && dialog.open) {
      dialog.close();
    }
  }, [preview]);
  const search = normalize(query.trim());
  const isFiltered = selectedMuscleFilter.id !== ALL_MUSCLE_FILTER_ID;
  const visibleExercises = exercises.filter(
    (exercise) =>
      matchesMuscleFilter(exercise.grupoMuscularPrincipal, selectedMuscleFilter) &&
      (!search ||
        normalize(exercise.nombre).includes(search) ||
        normalize(exercise.grupoMuscularPrincipal).includes(search)),
  );
  const previewHasDemo = preview
    ? hasEntrenateExerciseDemo(preview.nombre) || Boolean(preview.workoutGuideId)
    : false;

  const toggleSelection = (id: string) => {
    const next = new Set(selectedIds);
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    setSelectedIds(next);
    onSelectionChange?.(Array.from(next));
  };

  return (
    <div className={`${mode === "select" ? "mt-4" : "mt-5"} min-w-0`}>
      <label className="sr-only" htmlFor="exercise-search">
        Buscar ejercicio
      </label>
      <div className="relative">
        <Search aria-hidden="true" className="absolute top-1/2 left-3 -translate-y-1/2 text-text-secondary" size={16} />
        <input
          autoComplete="off"
          className="h-12 w-full rounded-control-sm border border-border/70 bg-surface/60 pr-12 pl-10 text-sm text-text-primary outline-none transition-colors placeholder:text-text-secondary/70 hover:border-border-strong focus:border-primary focus:ring-2 focus:ring-primary/25 focus-visible:outline-primary [&::-webkit-search-cancel-button]:appearance-none"
          id="exercise-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Buscar ejercicio..."
          type="search"
          value={query}
        />
        {query ? (
          <button
            aria-label="Limpiar búsqueda"
            className="absolute inset-y-0 right-0 flex w-12 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:text-text-primary focus-visible:outline-primary"
            onClick={() => setQuery("")}
            type="button"
          >
            <X aria-hidden="true" size={18} />
          </button>
        ) : null}
      </div>

      {exercises.length > 0 ? (
        <div className="relative mt-3">
          <div
            aria-label="Filtrar por grupo muscular"
            className="flex touch-pan-x gap-1.5 overflow-x-auto overscroll-x-contain pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
            role="group"
          >
            {muscleFilters.map((filter) => {
              const selected = selectedMuscleFilter.id === filter.id;
              return (
                <button
                  aria-label={`Filtrar por ${filter.label}`}
                  aria-pressed={selected}
                  className={`relative flex shrink-0 flex-col items-center justify-start rounded-control-sm px-1.5 pb-1.5 transition-[background-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-primary focus-visible:outline-offset-1 motion-reduce:transition-none motion-reduce:active:scale-100 ${mode === "select" ? "h-[5.25rem] min-w-[4.5rem]" : "h-[5.75rem] min-w-[4.75rem]"} ${selected ? "bg-primary/[0.06] text-text-primary" : "text-text-secondary hover:bg-surface/20 hover:text-text-primary"}`}
                  key={filter.id}
                  onClick={() => setMuscleFilterId(filter.id)}
                  type="button"
                >
                  <Image
                    alt=""
                    aria-hidden="true"
                    className={`${mode === "select" ? "size-12" : "size-14"} shrink-0 object-contain ${filter.id === ALL_MUSCLE_FILTER_ID ? "scale-[1.12]" : ""}`}
                    height={mode === "select" ? 48 : 56}
                    sizes={mode === "select" ? "48px" : "56px"}
                    src={filter.imageSrc}
                    width={mode === "select" ? 48 : 56}
                  />
                  <span className="mt-1 whitespace-nowrap text-[0.8125rem] font-semibold leading-4">{filter.label}</span>
                  <span aria-hidden="true" className={`absolute bottom-0 h-0.5 w-8 rounded-full bg-primary transition-opacity duration-150 motion-reduce:transition-none ${selected ? "opacity-100" : "opacity-0"}`} />
                </button>
              );
            })}
          </div>
        </div>
      ) : null}

      {exercises.length === 0 ? (
        <p className="mt-8 text-sm text-text-secondary">No hay ejercicios disponibles.</p>
      ) : visibleExercises.length === 0 ? (
        <div className="mt-8">
          <p className="text-sm text-text-secondary">No encontramos ejercicios con esos filtros.</p>
          <div className="mt-3 flex flex-wrap gap-3">
            {query ? (
              <button
                className="inline-flex min-h-11 items-center text-sm font-semibold text-primary transition-colors hover:text-text-primary focus-visible:outline-primary"
                onClick={() => setQuery("")}
                type="button"
              >
                Limpiar búsqueda
              </button>
            ) : null}
            {isFiltered ? (
              <button
                className="inline-flex min-h-11 items-center text-sm font-semibold text-primary transition-colors hover:text-text-primary focus-visible:outline-primary"
                onClick={() => setMuscleFilterId(ALL_MUSCLE_FILTER_ID)}
                type="button"
              >
                Ver todos
              </button>
            ) : null}
          </div>
        </div>
      ) : (
        <ul className={`${mode === "browse" ? "mt-4" : "mt-3"} divide-y divide-border/50 border-y border-border/50`}>
          {visibleExercises.map((exercise) => {
            const selected = mode === "select" && selectedIds.has(exercise.id);

            return (
              <li
                className={`min-w-0 transition-colors ${selected ? "bg-primary/5" : "bg-transparent"}`}
                key={exercise.id}
              >
                <button
                  aria-haspopup={mode === "browse" ? "dialog" : undefined}
                  aria-label={mode === "select" ? `${selected ? "Quitar" : "Seleccionar"} ${exercise.nombre}` : `Ver detalles de ${exercise.nombre}`}
                  aria-pressed={mode === "select" ? selected : undefined}
                  className="group flex min-h-[4.5rem] w-full min-w-0 items-center gap-2 px-1 py-3.5 text-left transition-[background-color,transform] duration-150 hover:bg-surface/25 active:scale-[0.995] focus-visible:outline-2 focus-visible:outline-primary motion-reduce:transition-none motion-reduce:active:scale-100"
                  onClick={() => mode === "select" ? toggleSelection(exercise.id) : setPreview(exercise)}
                  type="button"
                >
                  <span className="min-w-0 flex-1">
                    <span className="block break-words font-brand text-base font-bold leading-5 text-text-primary">
                      {exercise.nombre}
                    </span>
                    <span className="mt-1 block text-[0.8125rem] font-semibold leading-4 text-info">
                      {exercise.grupoMuscularPrincipal}
                    </span>
                  </span>
                  <span className={`flex size-11 shrink-0 items-center justify-center transition-colors ${selected ? "text-primary" : "text-text-secondary group-hover:text-text-primary"}`}>
                    {mode === "select"
                      ? selected
                        ? <Check aria-hidden="true" size={19} strokeWidth={2.25} />
                        : <Plus aria-hidden="true" size={19} strokeWidth={1.75} />
                      : <ChevronRight aria-hidden="true" size={18} strokeWidth={1.75} />}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      )}

      <dialog
        aria-labelledby="exercise-preview-title"
        aria-modal="true"
        className="fixed inset-x-0 top-auto bottom-0 m-0 max-h-[90dvh] w-full max-w-full overflow-y-auto rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:max-h-[calc(100dvh-2rem)] sm:w-[calc(100%-2rem)] sm:max-w-xl sm:rounded-container"
        onClick={(event) => {
          const dialog = event.currentTarget;
          const bounds = dialog.getBoundingClientRect();
          if (
            event.clientX < bounds.left || event.clientX > bounds.right ||
            event.clientY < bounds.top || event.clientY > bounds.bottom
          ) {
            dialog.close();
          }
        }}
        onClose={() => setPreview(null)}
        ref={dialogRef}
      >
        {preview ? (
          <div className="px-4 pt-2 pb-[calc(1.25rem+env(safe-area-inset-bottom))] sm:p-6">
            <div aria-hidden="true" className="mx-auto mb-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
            <div className="flex min-w-0 items-start gap-3">
              <div className="min-w-0 flex-1 pt-2">
                <h2 className="break-words font-brand text-xl font-bold leading-7 text-text-primary" id="exercise-preview-title">
                  {preview.nombre}
                </h2>
                <p className="mt-1 text-[0.8125rem] font-semibold text-info">
                  {preview.grupoMuscularPrincipal}
                </p>
              </div>
              <button
                aria-label="Cerrar vista previa"
                className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary transition-colors hover:bg-surface hover:text-text-primary focus-visible:outline-primary"
                onClick={() => dialogRef.current?.close()}
                type="button"
              >
                <X aria-hidden="true" size={20} />
              </button>
            </div>
            <p className="mt-4 text-[0.8125rem] font-semibold text-text-secondary/80">Equipamiento</p>
            <p className="mt-1 text-[0.8125rem] leading-5 text-text-secondary">
              {preview.equipamientos.length
                ? preview.equipamientos.map((item) => item.nombre).join(" · ")
                : "Sin equipamiento externo"}
            </p>
            {preview.descripcion?.trim() ? (
              <p className="mt-4 text-sm leading-6 text-text-secondary">{preview.descripcion}</p>
            ) : null}
            {previewHasDemo ? (
              <section aria-labelledby="exercise-technique-title" className="mt-8 border-t border-border/60 pt-5">
                <h3 className="font-brand text-lg font-bold text-text-primary" id="exercise-technique-title">Técnica</h3>
                <ExerciseMovementDemo
                  exerciseName={preview.nombre}
                  key={preview.id}
                  workoutGuideId={preview.workoutGuideId}
                />
              </section>
            ) : null}
          </div>
        ) : null}
      </dialog>
    </div>
  );
}
