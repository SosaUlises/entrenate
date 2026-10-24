import type { Exercise } from "@/features/exercises/types/exercise.types";

export type RoutineExerciseConfig = {
  cantidadSeries: number;
  repeticionesMinimas: number;
  repeticionesMaximas: number;
  rirObjetivoMinimo: number;
  rirObjetivoMaximo: number;
  descansoSegundos: number;
  notas: string | null;
};

export type RoutineDraftExercise = Pick<Exercise, "nombre"> &
  Partial<Pick<Exercise, "grupoMuscularPrincipal" | "equipamientos">> &
  RoutineExerciseConfig & { exerciseId: string };

export function createRoutineDraftExercise(exercise: Exercise): RoutineDraftExercise {
  return {
    exerciseId: exercise.id,
    nombre: exercise.nombre,
    grupoMuscularPrincipal: exercise.grupoMuscularPrincipal,
    equipamientos: exercise.equipamientos,
    cantidadSeries: 3,
    repeticionesMinimas: 8,
    repeticionesMaximas: 12,
    rirObjetivoMinimo: 1,
    rirObjetivoMaximo: 2,
    descansoSegundos: 120,
    notas: null,
  };
}

export type RoutineExerciseErrors = Partial<Record<keyof RoutineExerciseConfig, string>>;

export function validateRoutineExerciseConfig(config: RoutineExerciseConfig): RoutineExerciseErrors {
  const errors: RoutineExerciseErrors = {};
  const inRange = (value: number, min: number, max: number) =>
    Number.isInteger(value) && value >= min && value <= max;

  if (!inRange(config.cantidadSeries, 1, 20)) errors.cantidadSeries = "Ingresá entre 1 y 20 series.";
  if (!inRange(config.repeticionesMinimas, 1, 100)) errors.repeticionesMinimas = "Ingresá un mínimo entre 1 y 100.";
  if (!inRange(config.repeticionesMaximas, 1, 100)) errors.repeticionesMaximas = "Ingresá un máximo entre 1 y 100.";
  if (!errors.repeticionesMinimas && !errors.repeticionesMaximas && config.repeticionesMinimas > config.repeticionesMaximas) {
    errors.repeticionesMaximas = "El máximo debe ser mayor o igual al mínimo.";
  }
  if (!inRange(config.rirObjetivoMinimo, 0, 5)) errors.rirObjetivoMinimo = "Ingresá un RIR mínimo entre 0 y 5.";
  if (!inRange(config.rirObjetivoMaximo, 0, 5)) errors.rirObjetivoMaximo = "Ingresá un RIR máximo entre 0 y 5.";
  if (!errors.rirObjetivoMinimo && !errors.rirObjetivoMaximo && config.rirObjetivoMinimo > config.rirObjetivoMaximo) {
    errors.rirObjetivoMaximo = "El máximo debe ser mayor o igual al mínimo.";
  }
  if (!inRange(config.descansoSegundos, 15, 900)) errors.descansoSegundos = "Ingresá entre 15 y 900 segundos.";
  if (config.notas && config.notas.length > 500) errors.notas = "Las notas no pueden superar 500 caracteres.";

  return errors;
}
