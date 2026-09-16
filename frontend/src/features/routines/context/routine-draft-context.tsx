"use client";

import { createContext, useContext, useState, type ReactNode } from "react";
import type { Exercise } from "@/features/exercises/types/exercise.types";
import { createRoutineDraftExercise, type RoutineDraftExercise, type RoutineExerciseConfig } from "../types/routine-draft-exercise";

export type RoutineDraftDay = {
  id: string;
  nombre: string;
  orden: number;
  exercises: RoutineDraftExercise[];
};

type RoutineDraftContextValue = {
  nombre: string;
  setNombre: (nombre: string) => void;
  dias: RoutineDraftDay[];
  addDay: () => void;
  renameDay: (dayId: string, nombre: string) => void;
  removeDay: (dayId: string) => void;
  setDayExercises: (dayId: string, exercises: Exercise[]) => void;
  removeDayExercise: (dayId: string, exerciseId: string) => void;
  updateDayExercise: (dayId: string, exerciseId: string, config: RoutineExerciseConfig) => void;
};

const RoutineDraftContext = createContext<RoutineDraftContextValue | null>(null);

export function RoutineDraftProvider({ children }: { children: ReactNode }) {
  const [nombre, setNombre] = useState("");
  const [dias, setDias] = useState<RoutineDraftDay[]>([
    { id: "day-1", nombre: "Día 1", orden: 1, exercises: [] },
  ]);

  const addDay = () => {
    const id = crypto.randomUUID();
    setDias((current) => {
      const orden = current.length + 1;
      return [...current, { id, nombre: `Día ${orden}`, orden, exercises: [] }];
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
        exercises: uniqueExercises.map((exercise) =>
          existing.get(exercise.id) ?? createRoutineDraftExercise(exercise)),
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

  return (
    <RoutineDraftContext.Provider value={{
      nombre, setNombre, dias, addDay, renameDay, removeDay, setDayExercises, removeDayExercise, updateDayExercise,
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
