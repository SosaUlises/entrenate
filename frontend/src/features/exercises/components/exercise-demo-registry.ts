export type EntrenateExerciseDemoDefinition = {
  key: string;
  movementFrames: readonly [string, string, string];
  armPositionFrames: readonly [string, string];
};

type DemoLoader = () => Promise<EntrenateExerciseDemoDefinition>;

function normalizeExerciseName(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("es-AR")
    .trim()
    .replace(/\s+/g, " ");
}

const demoLoaders: Record<string, DemoLoader> = {
  "press banca con barra": () => import("./exercise-demos/press-banca-barra.demo")
    .then((module) => module.pressBancaBarraDemo),
};

export function hasEntrenateExerciseDemo(exerciseName: string): boolean {
  return normalizeExerciseName(exerciseName) in demoLoaders;
}

export function loadEntrenateExerciseDemo(
  exerciseName: string,
): Promise<EntrenateExerciseDemoDefinition> {
  const loader = demoLoaders[normalizeExerciseName(exerciseName)];
  return loader?.() ?? Promise.reject(new Error("No Entrenate demo for exercise"));
}
