"use client";

import { ArrowDown, ArrowRight, ArrowUp, ChevronRight, CircleHelp, Search, X } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { MainPageHeader } from "@/components/layout/main-page-header";
import { Button } from "@/components/ui/button";
import {
  ALL_MUSCLE_FILTER_ID,
  getVisibleMuscleFilters,
  matchesMuscleFilter,
} from "@/features/exercises/exercise-muscle-filters";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getExerciseProgressHubAction } from "../actions/get-exercise-progress-hub.action";
import { formatCompactProgressDate, formatProgressDate, formatProgressValue } from "../progress-formatters";
import type { ExerciseProgressBestSet, ExerciseProgressHub, ExerciseProgressHubItem } from "../types/progress.types";

type HubState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; progress: ExerciseProgressHub };

export function ProgressHubContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<HubState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [query, setQuery] = useState("");
  const [muscleFilterId, setMuscleFilterId] = useState(ALL_MUSCLE_FILTER_ID);
  const [showTrendHelp, setShowTrendHelp] = useState(false);

  useEffect(() => {
    let active = true;

    void getExerciseProgressHubAction().then((result) => {
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }
      setState(result);
    }, () => {
      if (active) setState({ status: "error" });
    });

    return () => { active = false; };
  }, [invalidateSession, requestKey, router]);

  const exercises = useMemo(
    () => (state.status === "ready" ? state.progress.ejercicios : []),
    [state],
  );
  const muscleFilters = useMemo(
    () => getVisibleMuscleFilters(exercises.map((exercise) => exercise.grupoMuscularPrincipal)),
    [exercises],
  );
  const selectedMuscleFilter =
    muscleFilters.find((filter) => filter.id === muscleFilterId) ?? muscleFilters[0];
  const normalizedQuery = normalize(query.trim());
  const visibleExercises = exercises.filter(
    (exercise) =>
      matchesMuscleFilter(exercise.grupoMuscularPrincipal, selectedMuscleFilter) &&
      (!normalizedQuery || normalize(exercise.nombre).includes(normalizedQuery)),
  );
  const hasActiveFilters = Boolean(query.trim() || selectedMuscleFilter.id !== ALL_MUSCLE_FILTER_ID);
  const clearFilters = () => {
    setQuery("");
    setMuscleFilterId(ALL_MUSCLE_FILTER_ID);
  };

  return (
    <section aria-labelledby="progress-hub-title" className="mx-auto w-full min-w-0 max-w-xl overflow-x-clip">
      <MainPageHeader
        description="Mirá cómo viene tu rendimiento."
        id="progress-hub-title"
        title="Progreso"
      />

      {state.status === "loading" ? (
        <HubSkeleton />
      ) : state.status === "error" ? (
        <div role="alert">
          <p className="text-base text-text-primary">No pudimos cargar tu progreso.</p>
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
      ) : exercises.length === 0 ? (
        <EmptyHub />
      ) : (
        <>
          <div className="relative">
            <Search aria-hidden="true" className="absolute top-1/2 left-4 -translate-y-1/2 text-steel" size={19} />
            <label className="sr-only" htmlFor="progress-search">Buscar ejercicio</label>
            <input
              autoComplete="off"
              className="h-14 w-full rounded-control border border-border bg-surface-interactive pr-12 pl-12 text-base text-text-primary caret-thermal-accent outline-none transition-[background-color,border-color] duration-200 placeholder:text-text-muted hover:border-border-strong hover:bg-surface-elevated focus:border-focus focus:bg-surface-elevated focus-visible:outline-focus motion-reduce:transition-none"
              id="progress-search"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Buscar ejercicio"
              type="search"
              value={query}
            />
            {query ? (
              <button
                aria-label="Limpiar búsqueda"
                className="absolute top-1/2 right-1 flex size-12 -translate-y-1/2 items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:bg-surface-active hover:text-text-primary focus-visible:outline-focus"
                onClick={() => setQuery("")}
                type="button"
              >
                <X aria-hidden="true" size={19} />
              </button>
            ) : null}
          </div>

          <div
            aria-label="Filtrar por grupo muscular"
            className="mt-4 flex w-full min-w-0 touch-pan-x gap-2.5 overflow-x-auto overscroll-x-contain pb-1.5 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
            role="group"
          >
            {muscleFilters.map((filter) => {
              const selected = selectedMuscleFilter.id === filter.id;
              return (
                <button
                  aria-label={`Filtrar por ${filter.label}`}
                  aria-pressed={selected}
                  className={`relative flex h-28 min-w-[5.75rem] w-max shrink-0 flex-col items-center justify-start rounded-control-sm px-2 pb-2 transition-[background-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-focus focus-visible:outline-offset-2 motion-reduce:transition-none motion-reduce:active:scale-100 ${selected ? "bg-surface-active/55 text-text-primary" : "text-text-secondary hover:bg-surface-interactive/55 hover:text-text-primary"}`}
                  key={filter.id}
                  onClick={() => setMuscleFilterId(filter.id)}
                  type="button"
                >
                  <Image
                    alt=""
                    aria-hidden="true"
                    className={`size-[4.75rem] shrink-0 object-contain ${filter.id === ALL_MUSCLE_FILTER_ID ? "scale-[1.14]" : ""}`}
                    height={76}
                    sizes="76px"
                    src={filter.imageSrc}
                    width={76}
                  />
                  <span className="mt-2 whitespace-nowrap text-[0.8125rem] leading-4 font-semibold">
                    {filter.label}
                  </span>
                  <span
                    aria-hidden="true"
                    className={`absolute bottom-0 h-0.5 w-8 rounded-full bg-thermal-accent transition-opacity duration-150 motion-reduce:transition-none ${selected ? "opacity-100" : "opacity-0"}`}
                  />
                </button>
              );
            })}
          </div>

          <div className="mt-7">
            <div className="flex min-h-11 items-center justify-between gap-4">
              <h2 className="font-brand text-lg font-bold text-text-primary">Ejercicios</h2>
              <button
                aria-controls="progress-trend-help"
                aria-expanded={showTrendHelp}
                className="-mr-1 inline-flex min-h-11 items-center gap-2 rounded-control-sm px-2 text-sm font-semibold text-steel transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-focus"
                onClick={() => setShowTrendHelp((current) => !current)}
                type="button"
              >
                <CircleHelp aria-hidden="true" size={18} />
                Tendencia
              </button>
            </div>
            {showTrendHelp ? (
              <p className="mb-2 max-w-md text-[0.8125rem] leading-5 text-text-secondary" id="progress-trend-help">
                Compara tu 1RM estimado actual con tu primer registro.
              </p>
            ) : null}
            {visibleExercises.length === 0 ? (
              <div className="mt-6">
                <p className="text-base leading-6 text-text-primary">No encontramos ejercicios con estos filtros.</p>
                {hasActiveFilters ? (
                  <button
                    className="mt-3 flex min-h-11 items-center text-sm font-semibold text-thermal-accent hover:text-text-primary focus-visible:outline-focus"
                    onClick={clearFilters}
                    type="button"
                  >
                    Limpiar filtros
                  </button>
                ) : null}
              </div>
            ) : (
              <div className="mt-3 divide-y divide-border/60 border-y border-border/60">
                {visibleExercises.map((exercise) => <ProgressRow exercise={exercise} key={exercise.ejercicioId} />)}
              </div>
            )}
          </div>
        </>
      )}
    </section>
  );
}

function ProgressRow({ exercise }: { exercise: ExerciseProgressHubItem }) {
  const displaySet = exercise.mejorSerieUltimaSesion ?? exercise.ultimaSerieRegistrada;

  return (
    <Link
      aria-label={formatProgressRowLabel(exercise, displaySet)}
      className="group flex min-h-24 min-w-0 items-center gap-4 px-1 py-4 transition-[background-color,transform] duration-150 hover:bg-surface-interactive/45 active:scale-[0.99] active:bg-surface-active/55 motion-reduce:transition-none motion-reduce:active:scale-100 focus-visible:outline-focus"
      href={`/exercises/${exercise.ejercicioId}/progress`}
    >
      <span aria-hidden="true" className="min-w-0 flex-1 self-stretch py-0.5">
        <span className="block break-words font-brand text-[1.125rem] font-bold leading-[1.35] text-text-primary">
          {exercise.nombre}
        </span>
        {displaySet ? (
          <span className="mt-1.5 block break-words font-brand text-lg font-bold leading-6 tabular-nums text-text-primary">
            {formatSet(displaySet)}
          </span>
        ) : null}
        <span className="mt-1 block text-sm font-medium leading-5 text-text-secondary">
          Último · {formatCompactProgressDate(exercise.ultimoEntrenamiento)}
        </span>
      </span>
      <span aria-hidden="true" className="flex shrink-0 flex-col items-end justify-center gap-3 py-0.5">
        <ProgressChange value={exercise.cambioPorcentual} />
        <ChevronRight className="text-text-muted transition-colors group-hover:text-thermal-accent" size={20} strokeWidth={1.75} />
      </span>
    </Link>
  );
}

function ProgressChange({ value }: { value: number | null }) {
  if (value === null) {
    return <span className="font-brand text-base font-bold text-text-secondary" title="Sin comparación disponible">—</span>;
  }

  const formattedValue = formatProgressValue(Math.abs(value));
  const Icon = value > 0 ? ArrowUp : value < 0 ? ArrowDown : ArrowRight;

  return (
    <span className="flex shrink-0 items-center gap-1 whitespace-nowrap font-brand text-[0.9375rem] font-bold tabular-nums text-steel">
      <Icon aria-hidden="true" size={17} strokeWidth={2.25} />
      {formattedValue}%
    </span>
  );
}

function EmptyHub() {
  return (
    <div className="border-t border-border/60 pt-7">
      <h2 className="font-brand text-xl font-bold leading-7 text-text-primary">Todavía no tenés ejercicios con registros.</h2>
      <p className="mt-2 max-w-md text-base leading-6 text-text-secondary">
        Cuando completes entrenamientos, vas a poder seguir tu evolución desde acá.
      </p>
      <Link className="mt-5 inline-flex min-h-11 items-center gap-1 text-base font-semibold text-thermal-accent transition-colors hover:text-text-primary focus-visible:outline-focus" href="/routines">
        Ver rutinas
        <ChevronRight aria-hidden="true" size={18} />
      </Link>
    </div>
  );
}

function HubSkeleton() {
  return (
    <div aria-label="Cargando progreso" className="animate-pulse" role="status">
      <div className="h-14 w-full rounded-control bg-surface-interactive" />
      <div className="mt-4 flex gap-2.5 overflow-hidden">
        {[0, 1, 2, 3].map((item) => (
          <div className="flex h-28 w-[5.75rem] shrink-0 flex-col items-center" key={item}>
            <div className="size-[4.75rem] rounded-control-sm bg-surface-elevated" />
            <div className="mt-2 h-3 w-12 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
      <div className="mt-7 flex items-center justify-between gap-3">
        <div className="h-5 w-24 rounded bg-surface-elevated" />
        <div className="h-11 w-24 rounded-control-sm bg-surface-elevated" />
      </div>
      <div className="mt-4 divide-y divide-border/50 border-y border-border/50">
        {[0, 1, 2].map((item) => (
          <div className="flex min-h-[5.5rem] items-center gap-3 py-3" key={item}>
            <div className="min-w-0 flex-1">
              <div className="h-5 w-4/5 rounded bg-surface-elevated" />
              <div className="mt-2 h-5 w-32 rounded bg-surface-elevated" />
              <div className="mt-2 h-4 w-24 rounded bg-surface-elevated" />
            </div>
            <div className="w-20 shrink-0">
              <div className="ml-auto h-4 w-14 rounded bg-surface-elevated" />
              <div className="mt-5 ml-auto h-5 w-5 rounded bg-surface-elevated" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function formatSet(set: ExerciseProgressBestSet): string {
  return set.peso > 0
    ? `${formatProgressValue(set.peso)} kg × ${set.repeticiones}`
    : `${set.repeticiones} ${set.repeticiones === 1 ? "rep" : "reps"}`;
}

function formatProgressRowLabel(
  exercise: ExerciseProgressHubItem,
  displaySet: ExerciseProgressBestSet | null,
): string {
  const performance = displaySet
    ? displaySet.peso > 0
      ? `${formatProgressValue(displaySet.peso)} kilos por ${displaySet.repeticiones} repeticiones`
      : `${displaySet.repeticiones} repeticiones`
    : "sin rendimiento disponible";
  const change = exercise.cambioPorcentual === null
    ? "sin comparación de tendencia"
    : exercise.cambioPorcentual > 0
      ? `más ${formatProgressValue(Math.abs(exercise.cambioPorcentual))} por ciento`
      : exercise.cambioPorcentual < 0
        ? `menos ${formatProgressValue(Math.abs(exercise.cambioPorcentual))} por ciento`
        : "sin cambios";

  return `${exercise.nombre}. Último rendimiento: ${performance}. Cambio de 1RM estimado desde el primer registro: ${change}. Último entrenamiento: ${formatProgressDate(exercise.ultimoEntrenamiento)}.`;
}

function normalize(value: string): string {
  return value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es-AR");
}
