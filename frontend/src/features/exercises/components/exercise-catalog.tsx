"use client";

import Image from "next/image";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, Check, Plus, Search } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { getExercisesAction, type GetExercisesActionResult } from "../actions/get-exercises.action";
import type { Exercise } from "../types/exercise.types";

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
  "peck deck": "/exercises/exercise-peck-deck.png",
  "flexiones": "/exercises/exercise-flexiones.png",
  "fondos en paralelas": "/exercises/exercise-fondos-paralelas.png",
  "curl con mancuernas": "/exercises/exercise-curl-mancuernas.png",
  "curl con barra ez": "/exercises/exercise-curl-barra-ez.png",
  "curl en polea": "/exercises/exercise-curl-polea.png",
  "press frances con barra ez": "/exercises/exercise-press-frances-barra-ez.png",
  "sentadilla con barra": "/exercises/exercise-sentadilla-barra.png",
  "jalon al pecho": "/exercises/exercise-jalon-pecho.png",
  "extension de triceps en polea": "/exercises/exercise-extension-triceps-polea.png",
  "crunch abdominal": "/exercises/exercise-crunch-abdominal.png",
};

function getExerciseImage(nombre: string): string | undefined {
  return exerciseImages[normalize(nombre).trim().replace(/\s+/g, " ")];
}

export function ExerciseCatalog() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<CatalogState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

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
    <section aria-labelledby="exercises-title" className="mx-auto w-full max-w-xl">
      <div className="flex items-center gap-1">
        <Link
          aria-label="Volver a inicio"
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary"
          href="/home"
        >
          <ArrowLeft aria-hidden="true" size={20} />
        </Link>
        <h2 className="font-brand text-xl font-bold text-text-primary" id="exercises-title">
          Ejercicios
        </h2>
      </div>
      <p className="mt-1 pl-12 text-sm text-text-secondary">
        Elegí los ejercicios que querés agregar.
      </p>

      {state.status === "loading" ? (
        <div className="flex min-h-48 items-center justify-center">
          <Spinner className="size-6 text-primary" label="Cargando ejercicios" />
        </div>
      ) : state.status === "error" ? (
        <div className="mt-8 rounded-card border border-border bg-surface p-5">
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
        <ExerciseSelector exercises={state.exercises} />
      )}
    </section>
  );
}

type ExerciseSelectorProps = {
  exercises: Exercise[];
  onSelectionChange?: (selectedIds: string[]) => void;
};

export function ExerciseSelector({ exercises, onSelectionChange }: ExerciseSelectorProps) {
  const [query, setQuery] = useState("");
  const [group, setGroup] = useState<string | null>(null);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set());

  const groups = useMemo(
    () =>
      Array.from(new Set(exercises.map((exercise) => exercise.grupoMuscularPrincipal)))
        .sort((a, b) => a.localeCompare(b, "es-AR")),
    [exercises],
  );
  const search = normalize(query.trim());
  const visibleExercises = exercises.filter(
    (exercise) =>
      (!group || exercise.grupoMuscularPrincipal === group) &&
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
    <div className="mt-5 min-w-0">
      <label className="sr-only" htmlFor="exercise-search">
        Buscar ejercicio
      </label>
      <div className="relative">
        <Search aria-hidden="true" className="absolute top-1/2 left-3 -translate-y-1/2 text-text-secondary" size={16} />
        <input
          autoComplete="off"
          className="h-10.5 w-full rounded-control-sm border border-border/70 bg-surface/70 pr-3 pl-9 text-sm text-text-primary outline-none transition-colors placeholder:text-text-secondary/70 hover:border-border-strong focus:border-primary focus:ring-2 focus:ring-primary/25 focus-visible:outline-primary"
          id="exercise-search"
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Buscar ejercicio..."
          type="search"
          value={query}
        />
      </div>

      {exercises.length > 0 ? (
        <div
          aria-label="Filtrar por grupo muscular"
          className="mt-3 flex gap-1.5 overflow-x-auto pb-1 [scrollbar-width:thin] [&::-webkit-scrollbar]:h-1 [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-primary/45"
          role="group"
        >
          {[null, ...groups].map((item) => (
            <button
              aria-pressed={group === item}
              className="flex min-h-11 shrink-0 items-center rounded-full focus-visible:outline-primary"
              key={item ?? "all"}
              onClick={() => setGroup(item)}
              type="button"
            >
              <span className={`flex h-8 items-center rounded-full border px-3 text-xs font-semibold transition-colors ${group === item ? "border-primary/40 bg-primary/12 text-primary" : "border-border/50 bg-surface/60 text-text-secondary hover:border-border hover:text-text-primary"}`}>
                {item ?? "Todos"}
              </span>
            </button>
          ))}
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
        <ul className="mt-2 space-y-1.5">
          {visibleExercises.map((exercise) => {
            const selected = selectedIds.has(exercise.id);
            const imageSrc = getExerciseImage(exercise.nombre);
            return (
              <li
                className={`flex min-w-0 items-center gap-2.5 rounded-control-sm border px-3 py-2 transition-colors ${selected ? "border-primary/30 bg-primary/5" : "border-border/60 bg-surface/55"}`}
                key={exercise.id}
              >
                {imageSrc ? (
                  <Image
                    alt=""
                    className="size-16 shrink-0 rounded-control-sm object-cover sm:size-19"
                    height={76}
                    sizes="(min-width: 640px) 76px, 64px"
                    src={imageSrc}
                    width={76}
                  />
                ) : null}
                <div className="min-w-0 flex-1">
                  <h3 className="font-brand text-base font-bold leading-5 text-text-primary">{exercise.nombre}</h3>
                  <p className="mt-1 text-sm font-medium leading-5 text-primary">{exercise.grupoMuscularPrincipal}</p>
                  <p className="mt-1 text-xs leading-5 text-text-secondary">
                    {exercise.equipamientos.length
                      ? exercise.equipamientos.map((item) => item.nombre).join(" · ")
                      : "Sin equipamiento externo"}
                  </p>
                </div>
                <button
                  aria-label={`${selected ? "Quitar" : "Seleccionar"} ${exercise.nombre}`}
                  aria-pressed={selected}
                  className="flex size-11 shrink-0 items-center justify-center rounded-full focus-visible:outline-primary"
                  onClick={() => toggleSelection(exercise.id)}
                  type="button"
                >
                  <span className={`flex size-9 items-center justify-center rounded-full border transition-colors ${selected ? "border-primary/40 bg-primary/12 text-primary" : "border-border bg-surface-elevated/70 text-text-secondary hover:border-primary/40 hover:text-text-primary"}`}>
                    {selected ? <Check aria-hidden="true" size={17} /> : <Plus aria-hidden="true" size={17} />}
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
