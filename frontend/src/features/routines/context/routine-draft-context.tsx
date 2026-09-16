"use client";

import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import type { Exercise } from "@/features/exercises/types/exercise.types";
import { createRoutineDraftExercise, type RoutineDraftExercise, type RoutineExerciseConfig } from "../types/routine-draft-exercise";
import type { RoutineDetail } from "../types/routine.types";

export type RoutineDraftDay = {
  id: string;
  nombre: string;
  descripcion: string | null;
  orden: number;
  exercises: RoutineDraftExercise[];
};

type RoutineDraftContextValue = {
  nombre: string;
  setNombre: (nombre: string) => void;
  descripcion: string | null;
  dias: RoutineDraftDay[];
  hydratedRoutineId: string | null;
  hydrateDraft: (routine: RoutineDetail) => void;
  addDay: () => void;
  renameDay: (dayId: string, nombre: string) => void;
  removeDay: (dayId: string) => void;
  setDayExercises: (dayId: string, exercises: Exercise[]) => void;
  removeDayExercise: (dayId: string, exerciseId: string) => void;
  updateDayExercise: (dayId: string, exerciseId: string, config: RoutineExerciseConfig) => void;
  resetDraft: () => void;
};

const RoutineDraftContext = createContext<RoutineDraftContextValue | null>(null);

export function RoutineDraftProvider({ children }: { children: ReactNode }) {
  const [nombre, setNombre] = useState("");
  const [descripcion, setDescripcion] = useState<string | null>(null);
  const [hydratedRoutineId, setHydratedRoutineId] = useState<string | null>(null);
  const [dias, setDias] = useState<RoutineDraftDay[]>([
    { id: "day-1", nombre: "Día 1", descripcion: null, orden: 1, exercises: [] },
  ]);

  const hydrateDraft = useCallback((routine: RoutineDetail) => {
    setNombre(routine.nombre);
    setDescripcion(routine.descripcion);
    setDias([...routine.dias].sort((first, second) => first.orden - second.orden).map((day) => ({
      id: day.id,
      nombre: day.nombre,
      descripcion: day.descripcion,
      orden: day.orden,
      exercises: [...day.ejercicios].sort((first, second) => first.orden - second.orden).map((exercise) => ({
        exerciseId: exercise.ejercicioId,
        nombre: exercise.nombre,
        cantidadSeries: exercise.cantidadSeries,
        repeticionesMinimas: exercise.repeticionesMinimas,
        repeticionesMaximas: exercise.repeticionesMaximas,
        rirObjetivoMinimo: exercise.rirObjetivoMinimo,
        rirObjetivoMaximo: exercise.rirObjetivoMaximo,
        descansoSegundos: exercise.descansoSegundos,
        notas: exercise.notas,
      })),
    })));
    setHydratedRoutineId(routine.id);
  }, []);

  const addDay = () => {
    const id = crypto.randomUUID();
    setDias((current) => {
      const orden = current.length + 1;
      return [...current, { id, nombre: `Día ${orden}`, descripcion: null, orden, exercises: [] }];
    });
  };

  const renameDay = (dayId: string, nextNombre: string) => {
    setDias((current) => current.map((day) =>
      day.id === dayId ? { ...day, nombre: nextNombre } : day,
    ));
  };

  const removeDay = (dayId: string) => {
    setDias((current) => current.length <= 1
      ? current
      : current.filter((day) => day.id !== dayId).map((day, index) => ({ ...day, orden: index + 1 })),
    );
  };

  const setDayExercises = (dayId: string, exercises: Exercise[]) => {
    const seen = new Set<string>();
    const uniqueExercises = exercises.filter((exercise) => {
      if (seen.has(exercise.id)) return false;
      seen.add(exercise.id);
      return true;
    });
    setDias((current) => current.map((day) => {
      if (day.id !== dayId) return day;
      const existing = new Map(day.exercises.map((exercise) => [exercise.exerciseId, exercise]));
      return {
        ...day,
        exercises: uniqueExercises.map((exercise) => {
          const previous = existing.get(exercise.id);
          return previous
            ? { ...previous, nombre: exercise.nombre, grupoMuscularPrincipal: exercise.grupoMuscularPrincipal, equipamientos: exercise.equipamientos }
            : createRoutineDraftExercise(exercise);
        }),
      };
    }));
  };

  const removeDayExercise = (dayId: string, exerciseId: string) => {
    setDias((current) => current.map((day) =>
      day.id === dayId
        ? { ...day, exercises: day.exercises.filter((exercise) => exercise.exerciseId !== exerciseId) }
        : day,
    ));
  };

  const updateDayExercise = (dayId: string, exerciseId: string, config: RoutineExerciseConfig) => {
    setDias((current) => current.map((day) =>
      day.id === dayId
        ? { ...day, exercises: day.exercises.map((exercise) =>
          exercise.exerciseId === exerciseId ? { ...exercise, ...config } : exercise,
        ) }
        : day,
    ));
  };

  const resetDraft = () => {
    setNombre("");
    setDescripcion(null);
    setHydratedRoutineId(null);
    setDias([{ id: "day-1", nombre: "Día 1", descripcion: null, orden: 1, exercises: [] }]);
  };

  return (
    <RoutineDraftContext.Provider value={{
      nombre, setNombre, descripcion, dias, hydratedRoutineId, hydrateDraft,
      addDay, renameDay, removeDay, setDayExercises, removeDayExercise, updateDayExercise, resetDraft,
    }}>
      {children}
    </RoutineDraftContext.Provider>
  );
}

export function useRoutineDraft(): RoutineDraftContextValue {
  const context = useContext(RoutineDraftContext);
  if (!context) {
    throw new Error("useRoutineDraft debe usarse dentro de RoutineDraftProvider.");
  }
  return context;
}
