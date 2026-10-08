"use client";

import { ArrowDown, ArrowRight, ArrowUp, ChevronRight, Search, X } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { getExerciseImage } from "@/features/exercises/components/exercise-catalog";
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
  const [muscleGroup, setMuscleGroup] = useState<string | null>(null);

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

  const exercises = state.status === "ready" ? state.progress.ejercicios : [];
  const muscleGroups = Array.from(new Set(exercises.map((exercise) => exercise.grupoMuscularPrincipal)))
    .filter(Boolean)
    .sort((first, second) => first.localeCompare(second, "es-AR"));
  const normalizedQuery = normalize(query.trim());
  const visibleExercises = exercises.filter(
    (exercise) =>
      (!muscleGroup || exercise.grupoMuscularPrincipal === muscleGroup) &&
      (!normalizedQuery || normalize(exercise.nombre).includes(normalizedQuery)),
  );
  const hasActiveFilters = Boolean(query.trim() || muscleGroup);
  const clearFilters = () => {
    setQuery("");
    setMuscleGroup(null);
  };

  return (
    <section aria-labelledby="progress-hub-title" className="mx-auto w-full min-w-0 max-w-xl overflow-x-clip">
      <header>
        <h1 className="font-brand text-3xl font-bold leading-tight text-text-primary" id="progress-hub-title">Progreso</h1>
        <p className="mt-2 text-base leading-6 text-text-secondary">Mirá cómo viene tu rendimiento.</p>
      </header>

      {state.status === "loading" ? (
        <HubSkeleton />
      ) : state.status === "error" ? (
        <div className="mt-8" role="alert">
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
          <div className="relative mt-5">
            <Search aria-hidden="true" className="absolute top-1/2 left-3.5 -translate-y-1/2 text-text-secondary" size={19} />
            <label className="sr-only" htmlFor="progress-search">Buscar ejercicio</label>
            <input
              autoComplete="off"
              className="h-12 w-full rounded-control-sm border border-border/70 bg-surface/60 pr-12 pl-11 text-base text-text-primary outline-none transition-colors placeholder:text-text-secondary hover:border-border-strong focus:border-primary focus:ring-2 focus:ring-primary/25 focus-visible:outline-primary"
              id="progress-search"
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Buscar ejercicio"
              type="search"
              value={query}
            />
            {query ? (
              <button
                aria-label="Limpiar búsqueda"
                className="absolute top-1/2 right-0 flex size-12 -translate-y-1/2 items-center justify-center rounded-control-sm text-text-secondary hover:text-text-primary focus-visible:outline-primary"
                onClick={() => setQuery("")}
                type="button"
              >
                <X aria-hidden="true" size={19} />
              </button>
            ) : null}
          </div>

          <div aria-label="Filtrar por grupo muscular" className="mt-2 flex min-w-0 gap-2 overflow-x-auto pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden" role="group">
            {[null, ...muscleGroups].map((group) => {
              const selected = muscleGroup === group;
              return (
                <button
                  aria-pressed={selected}
                  className={`flex min-h-11 shrink-0 items-center rounded-full border px-4 text-sm font-semibold transition-colors focus-visible:outline-primary ${selected ? "border-primary/50 bg-primary/12 text-primary" : "border-border/60 bg-surface/45 text-text-secondary hover:border-border-strong hover:text-text-primary"}`}
                  key={group ?? "all"}
                  onClick={() => setMuscleGroup(group)}
                  type="button"
                >
                  {group ?? "Todos"}
                </button>
              );
            })}
          </div>

          <div className="mt-5">
            <h2 className="text-sm font-semibold uppercase tracking-[0.09em] text-text-secondary">Tu progreso</h2>
            <p className="mt-1.5 max-w-md text-sm leading-5 text-text-secondary">
              La tendencia compara tu 1RM estimado con tu primer registro.
            </p>
            {visibleExercises.length === 0 ? (
              <div className="mt-6">
                <p className="text-base leading-6 text-text-primary">No encontramos ejercicios con esos filtros.</p>
                {hasActiveFilters ? (
                  <button
                    className="mt-3 flex min-h-11 items-center text-sm font-semibold text-primary hover:text-text-primary focus-visible:outline-primary"
                    onClick={clearFilters}
                    type="button"
                  >
                    Limpiar filtros
                  </button>
                ) : null}
              </div>
            ) : (
              <div className="mt-2 divide-y divide-border/60 border-y border-border/60">
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
  const imageSrc = getExerciseImage(exercise.nombre);

  return (
    <Link
      aria-label={formatProgressRowLabel(exercise, displaySet)}
      className="group flex min-h-24 min-w-0 items-center gap-3 py-3 transition-colors hover:bg-surface/25 active:bg-surface/40 motion-reduce:transition-none focus-visible:outline-primary"
      href={`/exercises/${exercise.ejercicioId}/progress`}
    >
      {imageSrc ? (
        <span aria-hidden="true" className="flex size-16 shrink-0 items-center justify-center rounded-control-sm bg-surface/45 p-1">
          <Image alt="" className="size-full object-contain" height={64} sizes="64px" src={imageSrc} width={64} />
        </span>
      ) : null}
      <span aria-hidden="true" className="min-w-0 flex-1 self-stretch py-0.5">
        <span className="block break-words font-brand text-[1.0625rem] font-bold leading-[1.35] text-text-primary transition-colors group-hover:text-primary">
          {exercise.nombre}
        </span>
        {displaySet ? (
          <span className="mt-1 block break-words font-brand text-lg font-bold leading-6 tabular-nums text-text-primary">
            {formatSet(displaySet)}
          </span>
        ) : null}
        <span className="mt-1 block text-sm font-medium leading-5 text-text-secondary">
          Último · {formatCompactProgressDate(exercise.ultimoEntrenamiento)}
        </span>
      </span>
      <span aria-hidden="true" className="flex w-18 shrink-0 self-stretch flex-col items-end justify-between py-0.5">
        <ProgressChange value={exercise.cambioPorcentual} />
        <MiniTrend points={exercise.recentTrend} />
        <ChevronRight className="text-text-secondary transition-colors group-hover:text-primary" size={21} strokeWidth={2} />
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
    <span className="flex shrink-0 items-center gap-1 font-brand text-base font-bold tabular-nums text-info">
      <Icon aria-hidden="true" size={17} strokeWidth={2.25} />
      {formattedValue}%
    </span>
  );
}

function MiniTrend({ points }: { points: ExerciseProgressHubItem["recentTrend"] }) {
  if (!points || points.length < 2) return <span className="h-5" />;

  const width = 64;
  const height = 20;
  const values = points.map((point) => point.value);
  const minimum = Math.min(...values);
  const maximum = Math.max(...values);
  const range = maximum - minimum;
  const coordinates = values.map((value, index) => {
    const x = points.length === 1 ? width / 2 : (index / (points.length - 1)) * width;
    const y = range === 0 ? height / 2 : height - ((value - minimum) / range) * (height - 4) - 2;
    return `${x},${y}`;
  }).join(" ");

  return (
    <svg className="h-5 w-16 text-info/75" fill="none" viewBox={`0 0 ${width} ${height}`}>
      <polyline points={coordinates} stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" vectorEffect="non-scaling-stroke" />
    </svg>
  );
}

function EmptyHub() {
  return (
    <div className="mt-10 border-t border-border/60 pt-7">
      <h2 className="font-brand text-xl font-bold leading-7 text-text-primary">Todavía no tenés ejercicios con registros.</h2>
      <p className="mt-2 max-w-md text-base leading-6 text-text-secondary">
        Cuando completes entrenamientos, vas a poder seguir tu evolución desde acá.
      </p>
      <Link className="mt-5 inline-flex min-h-11 items-center gap-1 text-base font-semibold text-primary focus-visible:outline-primary" href="/routines">
        Ver rutinas
        <ChevronRight aria-hidden="true" size={18} />
      </Link>
    </div>
  );
}

function HubSkeleton() {
  return (
    <div aria-label="Cargando progreso" className="mt-6 animate-pulse" role="status">
      <div className="h-12 w-full rounded-control-sm bg-surface-elevated" />
      <div className="mt-3 flex gap-2 overflow-hidden">
        <div className="h-11 w-20 shrink-0 rounded-full bg-surface-elevated" />
        <div className="h-11 w-24 shrink-0 rounded-full bg-surface-elevated" />
        <div className="h-11 w-28 shrink-0 rounded-full bg-surface-elevated" />
        <div className="h-11 w-24 shrink-0 rounded-full bg-surface-elevated" />
      </div>
      <div className="mt-7 h-4 w-28 rounded bg-surface-elevated" />
      <div className="mt-3 h-3 w-72 max-w-full rounded bg-surface-elevated" />
      <div className="mt-4 divide-y divide-border/50 border-y border-border/50">
        {[0, 1, 2].map((item) => (
          <div className="flex min-h-24 items-center gap-3 py-3" key={item}>
            <div className="size-16 shrink-0 rounded-control-sm bg-surface-elevated" />
            <div className="min-w-0 flex-1">
              <div className="h-5 w-4/5 rounded bg-surface-elevated" />
              <div className="mt-2 h-5 w-32 rounded bg-surface-elevated" />
              <div className="mt-2 h-4 w-24 rounded bg-surface-elevated" />
            </div>
            <div className="w-16 shrink-0">
              <div className="ml-auto h-4 w-14 rounded bg-surface-elevated" />
              <div className="mt-3 h-5 w-16 rounded bg-surface-elevated" />
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
