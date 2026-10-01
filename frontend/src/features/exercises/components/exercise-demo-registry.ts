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
  "elevaciones laterales con mancuernas": () => import("./exercise-demos/elevaciones-laterales-mancuerna.demo")
    .then((module) => module.elevacionesLateralesMancuernaDemo),
  "face pull": () => import("./exercise-demos/face-pull.demo")
    .then((module) => module.facePullDemo),
  "pike push-up": () => import("./exercise-demos/pike-pushup.demo")
    .then((module) => module.pikePushupDemo),
  "curl con mancuernas": () => import("./exercise-demos/curl-mancuerna.demo")
    .then((module) => module.curlMancuernaDemo),
  "curl con barra ez": () => import("./exercise-demos/curl-barra-ez.demo")
    .then((module) => module.curlBarraEzDemo),
  "curl en polea": () => import("./exercise-demos/curl-polea.demo")
    .then((module) => module.curlPoleaDemo),
  "extension de triceps en polea": () => import("./exercise-demos/extension-tricep-polea.demo")
    .then((module) => module.extensionTricepPoleaDemo),
  "press frances con barra ez": () => import("./exercise-demos/press-frances-barra-ez.demo")
    .then((module) => module.pressFrancesBarraEzDemo),
  "fondos en paralelas": () => import("./exercise-demos/fondo-paralelas.demo")
    .then((module) => module.fondoParalelasDemo),
  "sentadilla con barra": () => import("./exercise-demos/sentadilla-barra.demo")
    .then((module) => module.sentadillaBarraDemo),
  "sentadilla goblet": () => import("./exercise-demos/sentadilla-goblet.demo")
    .then((module) => module.sentadillaGobletDemo),
  "sentadilla con peso corporal": () => import("./exercise-demos/sentadilla-peso-corporal.demo")
    .then((module) => module.sentadillaPesoCorporalDemo),
  "prensa de piernas": () => import("./exercise-demos/prensa-pierna.demo")
    .then((module) => module.prensaPiernaDemo),
  "hack squat": () => import("./exercise-demos/hack-squat.demo")
    .then((module) => module.hackSquatDemo),
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
