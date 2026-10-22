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
import { hasEntrenateExerciseDemoV2 } from "./exercise-demo-v2-registry";

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
    <section aria-labelledby="exercises-title" className={`mx-auto w-full min-w-0 max-w-xl ${props.mode === "select" ? "pb-[calc(6rem+env(safe-area-inset-bottom))]" : ""}`}>
      {props.mode === "select" ? (
        <div className="flex items-center gap-1">
          <Link
            aria-label={backLabel}
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
            href={backHref}
          >
            <ArrowLeft aria-hidden="true" size={20} />
          </Link>
          <h1 className="font-brand text-xl font-bold text-text-primary" id="exercises-title">
            {title}
          </h1>
        </div>
      ) : (
        <div className="max-w-lg">
          <h1 className="font-brand text-[2rem] font-bold leading-[1.08] text-text-primary" id="exercises-title">
            {title}
          </h1>
        </div>
      )}
      <p className={`text-text-secondary ${props.mode === "select" ? "mt-1 pl-12 text-sm" : "mt-3 max-w-md text-[0.9375rem] leading-6"}`}>
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
        <div className="fixed inset-x-0 bottom-0 z-20 border-t border-border/70 bg-surface-elevated">
          <div className="mx-auto flex max-w-xl items-center justify-between gap-3 px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))] sm:px-0 sm:pb-3">
            <p aria-live="polite" className="min-w-0 text-[0.8125rem] font-semibold text-text-secondary">
              {selectedExercises.length === 1
                ? "1 seleccionado"
                : `${selectedExercises.length} seleccionados`}
            </p>
            <Button
              className="shrink-0"
              disabled={selectedExercises.length === 0}
              onClick={() => props.onConfirmSelection(selectedExercises)}
              size="md"
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
    <div aria-label="Cargando ejercicios" className={`${compact ? "mt-4" : "mt-6"} animate-pulse`} role="status">
      <div className={`${compact ? "h-12 rounded-control-sm" : "h-14 rounded-control"} w-full bg-surface-elevated`} />
      <div className={`${compact ? "mt-3 gap-1.5" : "mt-5 gap-2"} flex overflow-hidden`}>
        {[0, 1, 2, 3].map((item) => (
          <div className={`flex shrink-0 flex-col items-center ${compact ? "h-[5.25rem] min-w-[4.5rem]" : "h-24 min-w-20"}`} key={item}>
            <div className={compact ? "size-12 rounded-control-sm bg-surface-elevated" : "size-14 rounded-control-sm bg-surface-elevated"} />
            <div className={`${compact ? "mt-1" : "mt-2"} h-3 w-10 rounded bg-surface-elevated`} />
          </div>
        ))}
      </div>
      <div className={`${compact ? "mt-2" : "mt-5"} border-y border-border/50`}>
        {[0, 1, 2, 3].map((item) => (
          <div className={`flex items-center border-b border-border/50 last:border-b-0 ${compact ? "min-h-[4.5rem] py-3.5" : "min-h-20 py-4"}`} key={item}>
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
    ? hasEntrenateExerciseDemoV2(preview.nombre) ||
      Boolean(preview.workoutGuideId)
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
    <div className={`${mode === "select" ? "mt-4" : "mt-6"} min-w-0`}>
      <label className="sr-only" htmlFor="exercise-search">
        Buscar ejercicio
      </label>
      <div className="relative">
        <Search aria-hidden="true" className={`absolute top-1/2 -translate-y-1/2 ${mode === "select" ? "left-3 text-text-secondary" : "left-4 text-steel"}`} size={mode === "select" ? 16 : 18} strokeWidth={1.8} />
        <input
          autoComplete="off"
          className={`w-full border pr-12 text-text-primary outline-none transition-[background-color,border-color] duration-200 hover:border-border-strong [&::-webkit-search-cancel-button]:appearance-none ${mode === "select" ? "h-12 rounded-control-sm border-border bg-surface-interactive pl-10 text-sm caret-thermal-accent placeholder:text-text-muted focus:border-focus focus-visible:outline-none" : "h-14 rounded-control border-border-strong bg-surface-interactive pl-12 text-[0.9375rem] caret-thermal-accent placeholder:text-text-muted focus:border-focus focus:bg-surface-elevated focus-visible:outline-none"}`}
          id="exercise-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Buscar ejercicio..."
          type="search"
          value={query}
        />
        {query ? (
          <button
            aria-label="Limpiar búsqueda"
            className={`absolute inset-y-0 right-0 flex w-12 items-center justify-center text-text-secondary transition-colors hover:bg-surface-active hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus ${mode === "select" ? "rounded-control-sm" : "rounded-control"}`}
            onClick={() => setQuery("")}
            type="button"
          >
            <X aria-hidden="true" size={18} />
          </button>
        ) : null}
      </div>

      {exercises.length > 0 ? (
        <div className={`relative ${mode === "select" ? "mt-3" : "mt-5"}`}>
          <div
            aria-label="Filtrar por grupo muscular"
            className={`flex touch-pan-x overflow-x-auto overscroll-x-contain [scrollbar-width:none] [&::-webkit-scrollbar]:hidden ${mode === "select" ? "gap-1.5 pb-1" : "gap-2 pb-2"}`}
            role="group"
          >
            {muscleFilters.map((filter) => {
              const selected = selectedMuscleFilter.id === filter.id;
              return (
                <button
                  aria-label={`Filtrar por ${filter.label}`}
                  aria-pressed={selected}
                  className={`relative flex shrink-0 flex-col items-center justify-start px-1.5 transition-[background-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100 ${mode === "select" ? "h-[5.25rem] min-w-[4.5rem] rounded-control-sm pb-1.5" : "h-24 min-w-20 rounded-control pb-2"} ${selected ? "bg-surface-active/55 text-text-primary" : mode === "select" ? "text-text-secondary hover:bg-surface/20 hover:text-text-primary" : "text-text-secondary hover:bg-surface-interactive/55 hover:text-text-primary"}`}
                  key={filter.id}
                  onClick={() => setMuscleFilterId(filter.id)}
                  type="button"
                >
                  <Image
                    alt=""
                    aria-hidden="true"
                    className={`${mode === "select" ? "size-12" : "size-14"} shrink-0 object-contain ${filter.id === ALL_MUSCLE_FILTER_ID ? "scale-[1.12]" : ""} ${mode === "browse" ? `transition-opacity duration-150 motion-reduce:transition-none ${selected ? "opacity-100" : "opacity-70"}` : ""}`}
                    height={mode === "select" ? 48 : 56}
                    sizes={mode === "select" ? "48px" : "56px"}
                    src={filter.imageSrc}
                    width={mode === "select" ? 48 : 56}
                  />
                  <span className={`${mode === "select" ? "mt-1" : "mt-2"} whitespace-nowrap text-[0.8125rem] font-semibold leading-4`}>{filter.label}</span>
                  <span aria-hidden="true" className={`absolute bottom-0 h-0.5 rounded-full bg-thermal-accent transition-opacity duration-150 motion-reduce:transition-none ${mode === "select" ? "w-8" : "w-6"} ${selected ? "opacity-100" : "opacity-0"}`} />
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
                className="inline-flex min-h-11 items-center text-sm font-semibold text-text-primary transition-colors hover:text-thermal-accent focus-visible:outline-2 focus-visible:outline-focus"
                onClick={() => setQuery("")}
                type="button"
              >
                Limpiar búsqueda
              </button>
            ) : null}
            {isFiltered ? (
              <button
                className="inline-flex min-h-11 items-center text-sm font-semibold text-text-primary transition-colors hover:text-thermal-accent focus-visible:outline-2 focus-visible:outline-focus"
                onClick={() => setMuscleFilterId(ALL_MUSCLE_FILTER_ID)}
                type="button"
              >
                Ver todos
              </button>
            ) : null}
          </div>
        </div>
      ) : (
        <ul className={`${mode === "browse" ? "mt-5" : "mt-3"} divide-y divide-border/50 border-y border-border/50`}>
          {visibleExercises.map((exercise) => {
            const selected = mode === "select" && selectedIds.has(exercise.id);

            return (
              <li
                className={`min-w-0 transition-colors ${selected ? "bg-surface-active/40" : "bg-transparent"}`}
                key={exercise.id}
              >
                <button
                  aria-haspopup={mode === "browse" ? "dialog" : undefined}
                  aria-label={mode === "select" ? `${selected ? "Quitar" : "Seleccionar"} ${exercise.nombre}` : `Ver detalles de ${exercise.nombre}`}
                  aria-pressed={mode === "select" ? selected : undefined}
                  className={`group flex w-full min-w-0 items-center gap-2 text-left transition-[background-color,transform] duration-150 active:scale-[0.995] focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100 ${mode === "select" ? "min-h-[4.5rem] px-2 py-3.5 hover:bg-surface-interactive/45" : "min-h-20 px-1 py-4 hover:bg-surface-interactive/40 focus-visible:bg-surface-interactive/40"}`}
                  onClick={() => mode === "select" ? toggleSelection(exercise.id) : setPreview(exercise)}
                  type="button"
                >
                  <span className="min-w-0 flex-1">
                    <span className={`block break-words font-brand font-bold text-text-primary ${mode === "select" ? "text-base leading-5" : "text-[1.0625rem] leading-6"}`}>
                      {exercise.nombre}
                    </span>
                    <span className={`block text-[0.8125rem] font-semibold text-info ${mode === "select" ? "mt-1 leading-4" : "mt-1.5 leading-5"}`}>
                      {exercise.grupoMuscularPrincipal}
                    </span>
                  </span>
                  <span className={`flex size-11 shrink-0 items-center justify-center transition-colors ${selected ? "text-thermal-accent" : mode === "select" ? "text-text-muted group-hover:text-text-primary" : "text-text-muted group-hover:text-thermal-accent"}`}>
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
        className="fixed inset-x-0 top-auto bottom-0 m-0 max-h-[92dvh] w-full max-w-full overflow-y-auto rounded-t-container border border-border-strong bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:max-h-[calc(100dvh-2rem)] sm:w-[calc(100%-2rem)] sm:max-w-xl sm:rounded-container"
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
          <div className="px-5 pt-2 pb-[calc(1.5rem+env(safe-area-inset-bottom))] sm:p-6">
            <div aria-hidden="true" className="mx-auto mb-3 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
            <div className="flex min-w-0 items-start gap-4 border-b border-border/60 pb-4">
              <div className="min-w-0 flex-1 pt-2 sm:pt-1">
                <h2 className="break-words font-brand text-2xl font-bold leading-tight text-text-primary" id="exercise-preview-title">
                  {preview.nombre}
                </h2>
                <p className="mt-2 text-[0.8125rem] font-semibold leading-5 text-info">
                  {preview.grupoMuscularPrincipal}
                  <span aria-hidden="true" className="px-2 text-text-muted">·</span>
                  <span className="font-medium text-text-secondary">
                    {preview.equipamientos.length
                      ? preview.equipamientos.map((item) => item.nombre).join(" · ")
                      : "Sin equipamiento externo"}
                  </span>
                </p>
              </div>
              <button
                aria-label="Cerrar vista previa"
                className="flex size-11 shrink-0 items-center justify-center rounded-control-sm border border-border bg-surface-interactive text-text-secondary transition-colors hover:border-border-strong hover:bg-surface-active hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
                onClick={() => dialogRef.current?.close()}
                type="button"
              >
                <X aria-hidden="true" size={20} />
              </button>
            </div>
            {preview.descripcion?.trim() ? (
              <p className="mt-4 text-sm leading-6 text-text-secondary">{preview.descripcion}</p>
            ) : null}
            {previewHasDemo ? (
              <section aria-labelledby="exercise-technique-title" className={preview.descripcion?.trim() ? "mt-5" : "mt-4"}>
                <div className="flex items-center gap-3">
                  <span aria-hidden="true" className="h-0.5 w-5 rounded-full bg-thermal-accent" />
                  <h3 className="font-brand text-lg font-bold text-text-primary" id="exercise-technique-title">Técnica</h3>
                </div>
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
