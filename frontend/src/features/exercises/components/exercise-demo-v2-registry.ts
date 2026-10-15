import type { EntrenateExerciseDemoDefinition } from "./exercise-demo-registry";

function normalizeExerciseName(value: string): string {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLocaleLowerCase("es-AR")
    .trim()
    .replace(/\s+/g, " ");
}

const curlBarraEzV2: EntrenateExerciseDemoDefinition = {
  key: "curl-barra-ez-v2",
  movementFrames: [
    "/exercises/v2/curl-barra-ez/frame-01.png",
    "/exercises/v2/curl-barra-ez/frame-02.png",
    "/exercises/v2/curl-barra-ez/frame-03.png",
    "/exercises/v2/curl-barra-ez/frame-04.png",
    "/exercises/v2/curl-barra-ez/frame-05.png",
  ],
};

const pressBancaBarraV2: EntrenateExerciseDemoDefinition = {
  key: "press-banca-barra-v2",
  movementFrames: [
    "/exercises/v2/press-banca-barra/frame-01.png",
    "/exercises/v2/press-banca-barra/frame-02.png",
    "/exercises/v2/press-banca-barra/frame-03.png",
    "/exercises/v2/press-banca-barra/frame-04.png",
    "/exercises/v2/press-banca-barra/frame-05.png",
  ],
};

const sentadillaBarraV2: EntrenateExerciseDemoDefinition = {
  key: "sentadilla-barra-v2",
  movementFrames: [
    "/exercises/v2/sentadilla-barra/frame-01.png",
    "/exercises/v2/sentadilla-barra/frame-02.png",
    "/exercises/v2/sentadilla-barra/frame-03.png",
    "/exercises/v2/sentadilla-barra/frame-04.png",
    "/exercises/v2/sentadilla-barra/frame-05.png",
  ],
};

const pressInclinadoMancuernasV2: EntrenateExerciseDemoDefinition = {
  key: "press-inclinado-mancuernas-v2",
  movementFrames: [
    "/exercises/v2/press-inclinado-mancuernas/frame-01.png",
    "/exercises/v2/press-inclinado-mancuernas/frame-02.png",
    "/exercises/v2/press-inclinado-mancuernas/frame-03.png",
    "/exercises/v2/press-inclinado-mancuernas/frame-04.png",
    "/exercises/v2/press-inclinado-mancuernas/frame-05.png",
  ],
};

const jalonPechoV2: EntrenateExerciseDemoDefinition = {
  key: "jalon-pecho-v2",
  movementFrames: [
    "/exercises/v2/jalon-pecho/frame-01.png",
    "/exercises/v2/jalon-pecho/frame-02.png",
    "/exercises/v2/jalon-pecho/frame-03.png",
    "/exercises/v2/jalon-pecho/frame-04.png",
    "/exercises/v2/jalon-pecho/frame-05.png",
  ],
};

const demoDefinitions = new Map<string, EntrenateExerciseDemoDefinition>([
  ["curl con barra ez", curlBarraEzV2],
  ["press banca con barra", pressBancaBarraV2],
  ["sentadilla con barra", sentadillaBarraV2],
  ["press inclinado con mancuernas", pressInclinadoMancuernasV2],
  ["jalon al pecho", jalonPechoV2],
]);

export function getEntrenateExerciseDemoV2(
  exerciseName: string,
): EntrenateExerciseDemoDefinition | null {
  return demoDefinitions.get(normalizeExerciseName(exerciseName)) ?? null;
}
