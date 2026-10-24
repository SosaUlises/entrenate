"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ChevronRight, Search, X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  getExercisesAction,
  type GetExercisesActionResult,
} from "@/features/exercises/actions/get-exercises.action";
import {
  ALL_MUSCLE_FILTER_ID,
  getVisibleMuscleFilters,
  matchesMuscleFilter,
} from "@/features/exercises/exercise-muscle-filters";
import type { Exercise } from "@/features/exercises/types/exercise.types";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";

type EntryState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; exercises: Exercise[] };

const emptyExercises: Exercise[] = [];

export function ProgressCoachEntry() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<EntryState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [query, setQuery] = useState("");
  const [muscleFilterId, setMuscleFilterId] = useState(ALL_MUSCLE_FILTER_ID);

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

  const exercises =
    state.status === "ready" ? state.exercises : emptyExercises;
  const muscleFilters = useMemo(
    () =>
      getVisibleMuscleFilters(
        exercises.map((exercise) => exercise.grupoMuscularPrincipal),
      ),
    [exercises],
  );
  const selectedMuscleFilter =
    muscleFilters.find((filter) => filter.id === muscleFilterId) ??
    muscleFilters[0];
  const normalizedQuery = normalize(query.trim());
  const visibleExercises = selectedMuscleFilter
    ? exercises.filter(
        (exercise) =>
          matchesMuscleFilter(
            exercise.grupoMuscularPrincipal,
            selectedMuscleFilter,
          ) &&
          (!normalizedQuery ||
            normalize(exercise.nombre).includes(normalizedQuery) ||
            normalize(exercise.grupoMuscularPrincipal).includes(
              normalizedQuery,
            )),
      )
    : [];
  const hasActiveFilters =
    Boolean(query.trim()) || muscleFilterId !== ALL_MUSCLE_FILTER_ID;

  return (
    <section
      aria-labelledby="progress-coach-title"
      className="mx-auto w-full min-w-0 max-w-xl overflow-x-clip"
    >
      <header className="max-w-lg">
        <div className="flex min-w-0 items-center gap-1">
          <Link
            aria-label="Volver a Coach"
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
            href="/coach"
          >
            <ArrowLeft aria-hidden="true" size={19} />
          </Link>
          <h1
            className="min-w-0 font-brand text-2xl font-bold leading-tight text-text-primary"
            id="progress-coach-title"
          >
            Coach de progreso
          </h1>
        </div>
        <p className="mt-1.5 pl-12 text-sm leading-6 text-text-secondary">
          Eleg&iacute; un ejercicio para analizar su evoluci&oacute;n y pr&oacute;ximo paso.
        </p>
      </header>

      <div className="mt-7">
        {state.status === "loading" ? (
          <ExerciseSelectionSkeleton />
        ) : state.status === "error" ? (
          <div className="mt-6" role="alert">
            <p className="text-sm leading-6 text-text-primary">
              No pudimos cargar los ejercicios.
            </p>
            <Button
              className="mt-4"
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
          <p className="mt-6 text-sm leading-6 text-text-secondary">
            No hay ejercicios disponibles para analizar.
          </p>
        ) : (
          <>
            <div className="relative">
              <Search
                aria-hidden="true"
                className="absolute top-1/2 left-4 -translate-y-1/2 text-steel"
                size={18}
                strokeWidth={1.8}
              />
              <label className="sr-only" htmlFor="coach-exercise-search">
                Buscar ejercicio
              </label>
              <input
                autoComplete="off"
                className="h-14 w-full rounded-control border border-border-strong bg-surface-interactive pr-12 pl-12 text-[0.9375rem] text-text-primary caret-thermal-accent outline-none transition-[background-color,border-color] duration-200 placeholder:text-text-muted hover:border-border-strong focus:border-focus focus:bg-surface-elevated focus-visible:outline-none motion-reduce:transition-none [&::-webkit-search-cancel-button]:appearance-none"
                id="coach-exercise-search"
                onChange={(event) => setQuery(event.target.value)}
                placeholder="Buscar ejercicio"
                type="search"
                value={query}
              />
              {query ? (
                <button
                  aria-label={"Limpiar b\u00fasqueda"}
                  className="absolute inset-y-0 right-0 flex w-12 items-center justify-center rounded-control text-text-secondary transition-colors hover:bg-surface-active hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
                  onClick={() => setQuery("")}
                  type="button"
                >
                  <X aria-hidden="true" size={18} />
                </button>
              ) : null}
            </div>

            <div
              aria-label="Filtrar por grupo muscular"
              className="mt-4 flex touch-pan-x gap-1.5 overflow-x-auto overscroll-x-contain pb-1 [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
              role="group"
            >
              {muscleFilters.map((filter) => {
                const selected = filter.id === selectedMuscleFilter.id;

                return (
                  <button
                    aria-label={`Filtrar por ${filter.label}`}
                    aria-pressed={selected}
                    className={`relative flex h-[5.25rem] min-w-[4.5rem] shrink-0 flex-col items-center justify-start rounded-control-sm px-1.5 pb-1.5 transition-[background-color,color,transform] duration-150 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100 ${selected ? "bg-surface-active/55 text-text-primary" : "text-text-secondary hover:bg-surface/20 hover:text-text-primary"}`}
                    key={filter.id}
                    onClick={() => setMuscleFilterId(filter.id)}
                    type="button"
                  >
                    <Image
                      alt=""
                      aria-hidden="true"
                      className={`size-12 shrink-0 object-contain ${filter.id === ALL_MUSCLE_FILTER_ID ? "scale-[1.12]" : ""}`}
                      height={48}
                      sizes="48px"
                      src={filter.imageSrc}
                      width={48}
                    />
                    <span className="mt-1 whitespace-nowrap text-[0.8125rem] font-semibold leading-4">
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

            {visibleExercises.length === 0 ? (
              <div className="mt-7">
                <p className="text-sm leading-6 text-text-secondary">
                  No encontramos ejercicios con esos filtros.
                </p>
                {hasActiveFilters ? (
                  <button
                    className="mt-2 inline-flex min-h-11 items-center text-sm font-semibold text-text-primary transition-colors hover:text-thermal-accent focus-visible:outline-2 focus-visible:outline-focus"
                    onClick={() => {
                      setQuery("");
                      setMuscleFilterId(ALL_MUSCLE_FILTER_ID);
                    }}
                    type="button"
                  >
                    Ver todos
                  </button>
                ) : null}
              </div>
            ) : (
              <ul className="mt-3 divide-y divide-border/60 border-y border-border/60">
                {visibleExercises.map((exercise) => (
                  <li key={exercise.id}>
                    <Link
                      aria-label={`Analizar ${exercise.nombre}`}
                      className="group flex min-h-[4.75rem] min-w-0 items-center gap-2 rounded-control-sm px-1 py-4 transition-[background-color,transform] duration-150 hover:bg-surface-interactive/45 active:scale-[0.995] active:bg-surface-active/45 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100"
                      href={`/coach/progress/${exercise.id}`}
                    >
                      <span className="min-w-0 flex-1">
                        <span className="block break-words font-brand text-[1.0625rem] font-bold leading-6 text-text-primary">
                          {exercise.nombre}
                        </span>
                        <span className="mt-1 block text-[0.8125rem] font-semibold leading-4 text-info">
                          {exercise.grupoMuscularPrincipal}
                        </span>
                      </span>
                      <span
                        aria-hidden="true"
                        className="flex size-11 shrink-0 items-center justify-center text-text-muted transition-colors group-hover:text-text-secondary group-active:text-thermal-accent"
                      >
                        <ChevronRight size={18} strokeWidth={1.75} />
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </div>
    </section>
  );
}

function ExerciseSelectionSkeleton() {
  return (
    <div
      aria-label="Cargando ejercicios"
      className="mt-5 animate-pulse"
      role="status"
    >
      <div className="h-14 rounded-control bg-surface-elevated" />
      <div className="mt-4 flex gap-1.5 overflow-hidden">
        {[0, 1, 2, 3].map((item) => (
          <div
            className="flex h-[5.25rem] min-w-[4.5rem] shrink-0 flex-col items-center"
            key={item}
          >
            <div className="size-12 rounded-control-sm bg-surface-elevated" />
            <div className="mt-1 h-3 w-10 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
      <div className="mt-3 border-y border-border/50">
        {[0, 1, 2, 3].map((item) => (
          <div
            className="flex min-h-[4.5rem] items-center border-b border-border/50 py-3.5 last:border-b-0"
            key={item}
          >
            <div className="min-w-0 flex-1">
              <div className="h-4 w-3/4 rounded bg-surface-elevated" />
              <div className="mt-2 h-3 w-24 rounded bg-surface-elevated" />
            </div>
            <div className="size-5 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
    </div>
  );
}

function normalize(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("es-AR");
}
