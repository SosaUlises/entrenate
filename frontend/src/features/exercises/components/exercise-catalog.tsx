"use client";

import Image from "next/image";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, Check, ChevronRight, Plus, Search, X } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
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

const exerciseImages: Record<string, string> = {
  "press banca con barra": "/exercises/exercise-press-banca-barra.png",
  "press banca con mancuernas": "/exercises/exercise-press-banca-mancuernas.png",
  "press inclinado con mancuernas": "/exercises/exercise-press-inclinado-mancuernas.png",
  "aperturas con mancuernas": "/exercises/exercise-aperturas-mancuernas.png",
  "peck deck": "/exercises/exercise-peck-deck.png",
  "flexiones": "/exercises/exercise-flexiones.png",
  "dominadas": "/exercises/exercise-dominadas.png",
  "remo con barra": "/exercises/exercise-remo-barra.png",
  "remo con mancuerna": "/exercises/exercise-remo-mancuerna.png",
  "remo en polea baja": "/exercises/exercise-remo-polea-baja.png",
  "press militar con barra": "/exercises/exercise-press-militar-barra.png",
  "press de hombros con mancuernas": "/exercises/exercise-press-hombros-mancuernas.png",
  "elevaciones laterales con mancuernas": "/exercises/exercise-elevaciones-laterales-mancuernas.png",
  "face pull": "/exercises/exercise-face-pull.png",
  "pike push-up": "/exercises/exercise-pike-push-up.png",
  "fondos en paralelas": "/exercises/exercise-fondos-paralelas.png",
  "curl con mancuernas": "/exercises/exercise-curl-mancuernas.png",
  "curl con barra ez": "/exercises/exercise-curl-barra-ez.png",
  "curl en polea": "/exercises/exercise-curl-polea.png",
  "curl femoral": "/exercises/exercise-curl-femoral.png",
  "press frances con barra ez": "/exercises/exercise-press-frances-barra-ez.png",
  "sentadilla con barra": "/exercises/exercise-sentadilla-barra.png",
  "sentadilla goblet": "/exercises/exercise-sentadilla-goblet.png",
  "sentadilla con peso corporal": "/exercises/exercise-sentadilla-peso-corporal.png",
  "prensa de piernas": "/exercises/exercise-prensa-piernas.png",
  "hack squat": "/exercises/exercise-hack-squat.png",
  "extension de cuadriceps": "/exercises/exercise-extension-cuadriceps.png",
  "zancadas con mancuernas": "/exercises/exercise-zancadas-mancuernas.png",
  "peso muerto rumano con barra": "/exercises/exercise-peso-muerto-rumano-barra.png",
  "hip thrust con barra": "/exercises/exercise-hip-thrust-barra.png",
  "puente de gluteos": "/exercises/exercise-puente-gluteos.png",
  "elevacion de gemelos en maquina": "/exercises/exercise-elevacion-gemelos-maquina.png",
  "elevacion de gemelos de pie": "/exercises/exercise-elevacion-gemelos-pie.png",
  "plancha": "/exercises/exercise-plancha.png",
  "jalon al pecho": "/exercises/exercise-jalon-pecho.png",
  "extension de triceps en polea": "/exercises/exercise-extension-triceps-polea.png",
  "crunch abdominal": "/exercises/exercise-crunch-abdominal.png",
};

export function getExerciseImage(nombre: string): string | undefined {
  return exerciseImages[normalize(nombre).trim().replace(/\s+/g, " ")];
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
    <section aria-labelledby="exercises-title" className={`mx-auto w-full max-w-xl ${props.mode === "select" ? "pb-20" : ""}`}>
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
        props.mode === "select" ? <ExercisePickerSkeleton /> : (
          <div className="flex min-h-48 items-center justify-center">
            <Spinner className="size-6 text-primary" label="Cargando ejercicios" />
          </div>
        )
      ) : state.status === "error" ? (
        <div className={props.mode === "select" ? "mt-8" : "mt-8 rounded-card border border-border bg-surface p-5"}>
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
              className="min-h-12 rounded-control-sm disabled:border disabled:border-border/60 disabled:bg-surface/70 disabled:text-text-secondary disabled:opacity-100"
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

function ExercisePickerSkeleton() {
  return (
    <div aria-label="Cargando ejercicios" className="mt-4 animate-pulse" role="status">
      <div className="h-11 w-full rounded-control-sm bg-surface-elevated" />
      <div className="mt-3 flex gap-1.5 overflow-hidden">
        {[0, 1, 2, 3].map((item) => (
          <div className="flex h-[4.75rem] min-w-[4.5rem] shrink-0 flex-col items-center" key={item}>
            <div className="size-11 rounded-control-sm bg-surface-elevated" />
            <div className="mt-1 h-3 w-10 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
      <div className="mt-2 border-y border-border/50">
        {[0, 1, 2, 3].map((item) => (
          <div className="flex min-h-[4.5rem] items-center border-b border-border/50 py-2.5 last:border-b-0" key={item}>
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
  const [preview, setPreview] = useState<{ exercise: Exercise; imageSrc: string } | null>(null);
  const [filterEdges, setFilterEdges] = useState({ left: false, right: false });
  const filterRef = useRef<HTMLDivElement>(null);
  const dialogRef = useRef<HTMLDialogElement>(null);

  const updateFilterEdges = useCallback(() => {
    const container = filterRef.current;
    if (!container) return;

    const left = container.scrollLeft > 1;
    const right = container.scrollLeft + container.clientWidth < container.scrollWidth - 1;
    setFilterEdges((current) =>
      current.left === left && current.right === right ? current : { left, right },
    );
  }, []);

  const muscleFilters = useMemo(
    () => getVisibleMuscleFilters(exercises.map((exercise) => exercise.grupoMuscularPrincipal)),
    [exercises],
  );
  const selectedMuscleFilter =
    muscleFilters.find((filter) => filter.id === muscleFilterId) ?? muscleFilters[0];

  useEffect(() => {
    const container = filterRef.current;
    if (!container) return;

    const observer = new ResizeObserver(updateFilterEdges);
    observer.observe(container);
    const frame = requestAnimationFrame(updateFilterEdges);

    return () => {
      cancelAnimationFrame(frame);
      observer.disconnect();
    };
  }, [muscleFilters, updateFilterEdges]);

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
  const visibleExercises = exercises.filter(
    (exercise) =>
      matchesMuscleFilter(exercise.grupoMuscularPrincipal, selectedMuscleFilter) &&
      (!search ||
        normalize(exercise.nombre).includes(search) ||
        normalize(exercise.grupoMuscularPrincipal).includes(search)),
  );

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
          className={`w-full rounded-control-sm border pr-3 pl-9 text-sm text-text-primary outline-none transition-colors placeholder:text-text-secondary/70 hover:border-border-strong focus:border-primary focus:ring-2 focus:ring-primary/25 focus-visible:outline-primary ${mode === "select" ? "h-11 border-border/60 bg-surface/50" : "h-10.5 border-border/70 bg-surface/70"}`}
          id="exercise-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Buscar ejercicio..."
          type="search"
          value={query}
        />
      </div>

      {exercises.length > 0 ? (
        <div className="relative mt-3">
          <div
            aria-label="Filtrar por grupo muscular"
            className="flex touch-pan-x gap-1.5 overflow-x-auto overscroll-x-contain pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
            onScroll={updateFilterEdges}
            ref={filterRef}
            role="group"
          >
            {muscleFilters.map((filter) => {
              const selected = selectedMuscleFilter.id === filter.id;
              return (
                <button
                  aria-label={`Filtrar por ${filter.label}`}
                  aria-pressed={selected}
                  className={`relative flex h-[4.75rem] min-w-[4.5rem] shrink-0 flex-col items-center justify-start rounded-control-sm px-1.5 pb-1.5 transition-[background-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-primary focus-visible:outline-offset-1 motion-reduce:transition-none motion-reduce:active:scale-100 ${selected ? "bg-primary/[0.06] text-text-primary" : "text-text-secondary hover:bg-surface/20 hover:text-text-primary"}`}
                  key={filter.id}
                  onClick={() => setMuscleFilterId(filter.id)}
                  type="button"
                >
                  <Image
                    alt=""
                    aria-hidden="true"
                    className={`size-11 shrink-0 object-contain ${filter.id === ALL_MUSCLE_FILTER_ID ? "scale-[1.12]" : ""}`}
                    height={44}
                    sizes="44px"
                    src={filter.imageSrc}
                    width={44}
                  />
                  <span className="mt-1 whitespace-nowrap text-[0.8125rem] font-semibold leading-4">{filter.label}</span>
                  <span aria-hidden="true" className={`absolute bottom-0 h-0.5 w-8 rounded-full bg-primary transition-opacity duration-150 motion-reduce:transition-none ${selected ? "opacity-100" : "opacity-0"}`} />
                </button>
              );
            })}
          </div>
          {filterEdges.left ? (
            <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 left-0 w-6 bg-linear-to-r from-background to-transparent sm:hidden" />
          ) : null}
          {filterEdges.right ? (
            <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 right-0 w-6 bg-linear-to-l from-background to-transparent sm:hidden" />
          ) : null}
        </div>
      ) : null}

      {exercises.length === 0 ? (
        <p className="mt-8 text-sm text-text-secondary">No hay ejercicios disponibles.</p>
      ) : visibleExercises.length === 0 ? (
        <p className="mt-8 text-sm text-text-secondary">
          {query.trim()
            ? `No encontramos ejercicios para "${query.trim()}".`
            : "No hay ejercicios disponibles."}
        </p>
      ) : (
        <ul className={mode === "select" ? "mt-2" : "mt-2 space-y-1.5"}>
          {visibleExercises.map((exercise) => {
            const selected = mode === "select" && selectedIds.has(exercise.id);
            const imageSrc = getExerciseImage(exercise.nombre);

            if (mode === "select") {
              return (
                <li
                  className={`flex min-w-0 items-stretch border-b border-border/50 transition-colors first:border-t ${selected ? "bg-primary/5" : "bg-transparent"}`}
                  key={exercise.id}
                >
                  <button
                    aria-label={`${selected ? "Quitar" : "Seleccionar"} ${exercise.nombre}`}
                    aria-pressed={selected}
                    className="flex min-h-[4.5rem] min-w-0 flex-1 items-center gap-2 px-1 py-2.5 text-left focus-visible:outline-primary"
                    onClick={() => toggleSelection(exercise.id)}
                    type="button"
                  >
                    <span className="min-w-0 flex-1">
                      <span className={`block break-words font-brand text-base font-bold leading-5 ${selected ? "text-primary" : "text-text-primary"}`}>
                        {exercise.nombre}
                      </span>
                      <span className="mt-1 block text-[0.8125rem] font-semibold leading-4 text-info">{exercise.grupoMuscularPrincipal}</span>
                      <span className="mt-0.5 block break-words text-[0.8125rem] leading-4 text-text-secondary">
                        {exercise.equipamientos.length
                          ? exercise.equipamientos.map((item) => item.nombre).join(" · ")
                          : "Sin equipamiento externo"}
                      </span>
                    </span>
                    <span className={`flex size-11 shrink-0 items-center justify-center ${selected ? "text-primary" : "text-text-secondary"}`}>
                      {selected ? <Check aria-hidden="true" size={19} strokeWidth={2.25} /> : <Plus aria-hidden="true" size={19} strokeWidth={1.75} />}
                    </span>
                  </button>
                </li>
              );
            }

            return (
              <li
                className="relative isolate flex min-w-0 items-center gap-2.5 rounded-control-sm border border-border/60 bg-surface/55 px-3 py-2 transition-colors"
                key={exercise.id}
              >
                {imageSrc ? (
                  <>
                    <button
                      aria-label={`Ver imagen de ${exercise.nombre}`}
                      className="absolute inset-0 rounded-control-sm focus-visible:outline-primary"
                      onClick={() => setPreview({ exercise, imageSrc })}
                      type="button"
                    />
                    <Image
                      alt=""
                      className="pointer-events-none relative z-10 size-16 shrink-0 rounded-control-sm object-cover sm:size-19"
                      height={76}
                      sizes="(min-width: 640px) 76px, 64px"
                      src={imageSrc}
                      width={76}
                    />
                  </>
                ) : null}
                <div className="pointer-events-none relative z-10 min-w-0 flex-1">
                  <h3 className="font-brand text-base font-bold leading-5 text-text-primary">{exercise.nombre}</h3>
                  <p className="mt-1 text-sm font-medium leading-5 text-primary">{exercise.grupoMuscularPrincipal}</p>
                  <p className="mt-1 text-[0.8125rem] leading-5 text-text-secondary">
                    {exercise.equipamientos.length
                      ? exercise.equipamientos.map((item) => item.nombre).join(" · ")
                      : "Sin equipamiento externo"}
                  </p>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <dialog
        aria-labelledby="exercise-preview-title"
        aria-modal="true"
        className="m-auto max-h-[calc(100dvh-1.5rem)] w-[calc(100%-1.5rem)] max-w-xl overflow-y-auto rounded-card border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:w-full"
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
          <div className="p-4 sm:p-6">
            <div className="relative mx-auto w-fit max-w-full">
              <Image
                alt={preview.exercise.nombre}
                className="block h-auto w-auto max-h-[calc(100dvh-13rem)] max-w-full rounded-control-sm object-contain"
                height={1254}
                sizes="(min-width: 640px) 528px, calc(100vw - 3.5rem)"
                src={preview.imageSrc}
                width={1254}
              />
              <button
                aria-label="Cerrar vista previa"
                className="absolute top-2 right-2 z-10 flex size-11 items-center justify-center rounded-full border border-border bg-surface/90 text-text-primary hover:border-border-strong focus-visible:outline-primary"
                onClick={() => dialogRef.current?.close()}
                type="button"
              >
                <X aria-hidden="true" size={20} />
              </button>
            </div>
            <h2 className="mt-5 font-brand text-xl font-bold text-text-primary" id="exercise-preview-title">
              {preview.exercise.nombre}
            </h2>
            <p className="mt-1 text-sm font-semibold text-primary">
              {preview.exercise.grupoMuscularPrincipal}
            </p>
            <p className="mt-2 text-sm leading-6 text-text-secondary">
              {preview.exercise.equipamientos.length
                ? preview.exercise.equipamientos.map((item) => item.nombre).join(" · ")
                : "Sin equipamiento externo"}
            </p>
            {mode === "browse" ? (
              <Link
                className="mt-3 inline-flex min-h-11 items-center gap-1 text-sm font-semibold text-primary hover:text-text-primary focus-visible:outline-primary"
                href={`/exercises/${preview.exercise.id}/progress`}
              >
                Ver progreso
                <ChevronRight aria-hidden="true" size={17} />
              </Link>
            ) : null}
            {hasEntrenateExerciseDemo(preview.exercise.nombre) || preview.exercise.workoutGuideId ? (
              <ExerciseMovementDemo
                exerciseName={preview.exercise.nombre}
                key={preview.exercise.id}
                workoutGuideId={preview.exercise.workoutGuideId}
              />
            ) : null}
          </div>
        ) : null}
      </dialog>
    </div>
  );
}
