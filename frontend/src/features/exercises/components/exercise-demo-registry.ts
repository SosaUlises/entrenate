export type EntrenateExerciseDemoDefinition = {
  key: string;
  movementFrames: readonly string[];
  armPositionFrames?: readonly string[];
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
  "press banca con mancuernas": () => import("./exercise-demos/press-banca-mancuernas.demo")
    .then((module) => module.pressBancaMancuernasDemo),
  "press inclinado con mancuernas": () => import("./exercise-demos/press-inclinado-mancuernas.demo")
    .then((module) => module.pressInclinadoMancuernasDemo),
  "aperturas con mancuernas": () => import("./exercise-demos/aperturas-mancuernas.demo")
    .then((module) => module.aperturasMancuernasDemo),
  "peck deck": () => import("./exercise-demos/peck-deck.demo")
    .then((module) => module.peckDeckDemo),
  "flexiones": () => import("./exercise-demos/flexiones.demo")
    .then((module) => module.flexionesDemo),
  "dominadas": () => import("./exercise-demos/dominadas.demo")
    .then((module) => module.dominadasDemo),
  "remo con barra": () => import("./exercise-demos/remo-barra.demo")
    .then((module) => module.remoBarraDemo),
  "remo con mancuerna": () => import("./exercise-demos/remo-mancuerna.demo")
    .then((module) => module.remoMancuernaDemo),
  "jalon al pecho": () => import("./exercise-demos/jalon-pecho.demo")
    .then((module) => module.jalonPechoDemo),
  "remo en polea baja": () => import("./exercise-demos/remo-polea-baja.demo")
    .then((module) => module.remoPoleaBajaDemo),
  "press militar con barra": () => import("./exercise-demos/press-militar-barra.demo")
    .then((module) => module.pressMilitarBarraDemo),
  "press de hombros con mancuernas": () => import("./exercise-demos/press-militar-mancuerna.demo")
    .then((module) => module.pressMilitarMancuernaDemo),
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
