import type { RoutineDraftDay } from "./context/routine-draft-context";
import type { CreateRoutineRequest } from "./types/routine.types";
import { validateRoutineExerciseConfig } from "./types/routine-draft-exercise";

type RoutineDraftInput = {
  nombre: string;
  descripcion: string | null;
  dias: RoutineDraftDay[];
};

export function mapRoutineDraftToRequest(draft: RoutineDraftInput): CreateRoutineRequest | null {
  if (draft.nombre.trim().length === 0 || draft.nombre.length > 100 ||
      (draft.descripcion?.length ?? 0) > 500 || draft.dias.length === 0 ||
      !draft.dias.some((day) => day.exercises.length > 0)) {
    return null;
  }

  if (draft.dias.some((day) =>
    day.nombre.trim().length === 0 || day.nombre.length > 100 ||
    (day.descripcion?.length ?? 0) > 500 ||
    day.exercises.some((exercise) =>
      exercise.exerciseId.trim().length === 0 ||
      Object.keys(validateRoutineExerciseConfig(exercise)).length > 0,
    ),
  )) {
    return null;
  }

  return {
    nombre: draft.nombre.trim(),
    descripcion: draft.descripcion?.trim() || null,
    dias: [...draft.dias]
      .sort((first, second) => first.orden - second.orden)
      .map((day, dayIndex) => ({
        nombre: day.nombre.trim(),
        descripcion: day.descripcion?.trim() || null,
        orden: dayIndex + 1,
        ejercicios: day.exercises.map((exercise, exerciseIndex) => ({
          ejercicioId: exercise.exerciseId,
          cantidadSeries: exercise.cantidadSeries,
          repeticionesMinimas: exercise.repeticionesMinimas,
          repeticionesMaximas: exercise.repeticionesMaximas,
          rirObjetivoMinimo: exercise.rirObjetivoMinimo,
          rirObjetivoMaximo: exercise.rirObjetivoMaximo,
          descansoSegundos: exercise.descansoSegundos,
          orden: exerciseIndex + 1,
          notas: exercise.notas?.trim() || null,
        })),
      })),
  };
}
