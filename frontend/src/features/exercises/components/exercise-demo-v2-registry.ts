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

const remoBarraV2: EntrenateExerciseDemoDefinition = {
  key: "remo-barra-v2",
  movementFrames: [
    "/exercises/v2/remo-barra/frame-01.png",
    "/exercises/v2/remo-barra/frame-02.png",
    "/exercises/v2/remo-barra/frame-03.png",
    "/exercises/v2/remo-barra/frame-04.png",
    "/exercises/v2/remo-barra/frame-05.png",
  ],
};

const remoMancuernaV2: EntrenateExerciseDemoDefinition = {
  key: "remo-mancuerna-v2",
  movementFrames: [
    "/exercises/v2/remo-mancuerna/frame-01.png",
    "/exercises/v2/remo-mancuerna/frame-02.png",
    "/exercises/v2/remo-mancuerna/frame-03.png",
    "/exercises/v2/remo-mancuerna/frame-04.png",
    "/exercises/v2/remo-mancuerna/frame-05.png",
  ],
};

const remoPoleaBajaV2: EntrenateExerciseDemoDefinition = {
  key: "remo-polea-baja-v2",
  movementFrames: [
    "/exercises/v2/remo-polea-baja/frame-01.png",
    "/exercises/v2/remo-polea-baja/frame-02.png",
    "/exercises/v2/remo-polea-baja/frame-03.png",
    "/exercises/v2/remo-polea-baja/frame-04.png",
    "/exercises/v2/remo-polea-baja/frame-05.png",
  ],
};

const dominadasV2: EntrenateExerciseDemoDefinition = {
  key: "dominadas-v2",
  movementFrames: [
    "/exercises/v2/dominadas/frame-01.png",
    "/exercises/v2/dominadas/frame-02.png",
    "/exercises/v2/dominadas/frame-03.png",
    "/exercises/v2/dominadas/frame-04.png",
    "/exercises/v2/dominadas/frame-05.png",
  ],
};

const facePullV2: EntrenateExerciseDemoDefinition = {
  key: "face-pull-v2",
  movementFrames: [
    "/exercises/v2/face-pull/frame-01.png",
    "/exercises/v2/face-pull/frame-02.png",
    "/exercises/v2/face-pull/frame-03.png",
    "/exercises/v2/face-pull/frame-04.png",
    "/exercises/v2/face-pull/frame-05.png",
  ],
};

const pressBancaMancuernasV2: EntrenateExerciseDemoDefinition = {
  key: "press-banca-mancuernas-v2",
  movementFrames: [
    "/exercises/v2/press-banca-mancuernas/frame-01.png",
    "/exercises/v2/press-banca-mancuernas/frame-02.png",
    "/exercises/v2/press-banca-mancuernas/frame-03.png",
    "/exercises/v2/press-banca-mancuernas/frame-04.png",
    "/exercises/v2/press-banca-mancuernas/frame-05.png",
  ],
};

const aperturaMancuernaV2: EntrenateExerciseDemoDefinition = {
  key: "apertura-mancuerna-v2",
  movementFrames: [
    "/exercises/v2/apertura-mancuerna/frame-01.png",
    "/exercises/v2/apertura-mancuerna/frame-02.png",
    "/exercises/v2/apertura-mancuerna/frame-03.png",
    "/exercises/v2/apertura-mancuerna/frame-04.png",
    "/exercises/v2/apertura-mancuerna/frame-05.png",
  ],
};

const peckDeckV2: EntrenateExerciseDemoDefinition = {
  key: "peck-deck-v2",
  movementFrames: [
    "/exercises/v2/peck-deck/frame-01.png",
    "/exercises/v2/peck-deck/frame-02.png",
    "/exercises/v2/peck-deck/frame-03.png",
    "/exercises/v2/peck-deck/frame-04.png",
    "/exercises/v2/peck-deck/frame-05.png",
  ],
};

const flexionesV2: EntrenateExerciseDemoDefinition = {
  key: "flexiones-v2",
  movementFrames: [
    "/exercises/v2/flexiones/frame-01.png",
    "/exercises/v2/flexiones/frame-02.png",
    "/exercises/v2/flexiones/frame-03.png",
    "/exercises/v2/flexiones/frame-04.png",
    "/exercises/v2/flexiones/frame-05.png",
  ],
};

const pressMilitarBarraV2: EntrenateExerciseDemoDefinition = {
  key: "press-militar-barra-v2",
  movementFrames: [
    "/exercises/v2/press-militar-barra/frame-01.png",
    "/exercises/v2/press-militar-barra/frame-02.png",
    "/exercises/v2/press-militar-barra/frame-03.png",
    "/exercises/v2/press-militar-barra/frame-04.png",
    "/exercises/v2/press-militar-barra/frame-05.png",
  ],
};

const pressHombroMancuernasV2: EntrenateExerciseDemoDefinition = {
  key: "press-hombro-mancuernas-v2",
  movementFrames: [
    "/exercises/v2/press-hombro-mancuernas/frame-01.png",
    "/exercises/v2/press-hombro-mancuernas/frame-02.png",
    "/exercises/v2/press-hombro-mancuernas/frame-03.png",
    "/exercises/v2/press-hombro-mancuernas/frame-04.png",
    "/exercises/v2/press-hombro-mancuernas/frame-05.png",
  ],
};

const pikePushUpV2: EntrenateExerciseDemoDefinition = {
  key: "pike-push-up-v2",
  movementFrames: [
    "/exercises/v2/pike-push-up/frame-01.png",
    "/exercises/v2/pike-push-up/frame-02.png",
    "/exercises/v2/pike-push-up/frame-03.png",
    "/exercises/v2/pike-push-up/frame-04.png",
    "/exercises/v2/pike-push-up/frame-05.png",
  ],
};

const elevacionesLateralesMancuernasV2: EntrenateExerciseDemoDefinition = {
  key: "elevaciones-laterales-mancuernas-v2",
  movementFrames: [
    "/exercises/v2/elevaciones-laterales-mancuernas/frame-01.png",
    "/exercises/v2/elevaciones-laterales-mancuernas/frame-02.png",
    "/exercises/v2/elevaciones-laterales-mancuernas/frame-03.png",
    "/exercises/v2/elevaciones-laterales-mancuernas/frame-04.png",
    "/exercises/v2/elevaciones-laterales-mancuernas/frame-05.png",
  ],
};

const curlMancuernasV2: EntrenateExerciseDemoDefinition = {
  key: "curl-mancuernas-v2",
  movementFrames: [
    "/exercises/v2/curl-mancuernas/frame-01.png",
    "/exercises/v2/curl-mancuernas/frame-02.png",
    "/exercises/v2/curl-mancuernas/frame-03.png",
    "/exercises/v2/curl-mancuernas/frame-04.png",
    "/exercises/v2/curl-mancuernas/frame-05.png",
  ],
};

const curlPoleaV2: EntrenateExerciseDemoDefinition = {
  key: "curl-polea-v2",
  movementFrames: [
    "/exercises/v2/curl-polea/frame-01.png",
    "/exercises/v2/curl-polea/frame-02.png",
    "/exercises/v2/curl-polea/frame-03.png",
    "/exercises/v2/curl-polea/frame-04.png",
    "/exercises/v2/curl-polea/frame-05.png",
  ],
};

const extensionTricepsPoleaV2: EntrenateExerciseDemoDefinition = {
  key: "extension-triceps-polea-v2",
  movementFrames: [
    "/exercises/v2/extension-triceps-polea/frame-01.png",
    "/exercises/v2/extension-triceps-polea/frame-02.png",
    "/exercises/v2/extension-triceps-polea/frame-03.png",
    "/exercises/v2/extension-triceps-polea/frame-04.png",
    "/exercises/v2/extension-triceps-polea/frame-05.png",
  ],
};

const pressFrancesBarraEzV2: EntrenateExerciseDemoDefinition = {
  key: "press-frances-barra-ez-v2",
  movementFrames: [
    "/exercises/v2/press-frances-barra-ez/frame-01.png",
    "/exercises/v2/press-frances-barra-ez/frame-02.png",
    "/exercises/v2/press-frances-barra-ez/frame-03.png",
    "/exercises/v2/press-frances-barra-ez/frame-04.png",
    "/exercises/v2/press-frances-barra-ez/frame-05.png",
  ],
};

const fondosParalelasV2: EntrenateExerciseDemoDefinition = {
  key: "fondos-paralelas-v2",
  movementFrames: [
    "/exercises/v2/fondos-paralelas/frame-01.png",
    "/exercises/v2/fondos-paralelas/frame-02.png",
    "/exercises/v2/fondos-paralelas/frame-03.png",
    "/exercises/v2/fondos-paralelas/frame-04.png",
    "/exercises/v2/fondos-paralelas/frame-05.png",
  ],
};

const sentadillaGobletV2: EntrenateExerciseDemoDefinition = {
  key: "sentadilla-goblet-v2",
  movementFrames: [
    "/exercises/v2/sentadilla-goblet/frame-01.png",
    "/exercises/v2/sentadilla-goblet/frame-02.png",
    "/exercises/v2/sentadilla-goblet/frame-03.png",
    "/exercises/v2/sentadilla-goblet/frame-04.png",
    "/exercises/v2/sentadilla-goblet/frame-05.png",
  ],
};

const sentadillaPesoCorporalV2: EntrenateExerciseDemoDefinition = {
  key: "sentadilla-peso-corporal-v2",
  movementFrames: [
    "/exercises/v2/sentadilla-peso-corporal/frame-01.png",
    "/exercises/v2/sentadilla-peso-corporal/frame-02.png",
    "/exercises/v2/sentadilla-peso-corporal/frame-03.png",
    "/exercises/v2/sentadilla-peso-corporal/frame-04.png",
    "/exercises/v2/sentadilla-peso-corporal/frame-05.png",
  ],
};

const prensaV2: EntrenateExerciseDemoDefinition = {
  key: "prensa-v2",
  movementFrames: [
    "/exercises/v2/prensa/frame-01.png",
    "/exercises/v2/prensa/frame-02.png",
    "/exercises/v2/prensa/frame-03.png",
    "/exercises/v2/prensa/frame-04.png",
    "/exercises/v2/prensa/frame-05.png",
  ],
};

const hackSquatV2: EntrenateExerciseDemoDefinition = {
  key: "hack-squat-v2",
  movementFrames: [
    "/exercises/v2/hack-squat/frame-01.png",
    "/exercises/v2/hack-squat/frame-02.png",
    "/exercises/v2/hack-squat/frame-03.png",
    "/exercises/v2/hack-squat/frame-04.png",
    "/exercises/v2/hack-squat/frame-05.png",
  ],
};

const extensionCuadricepsV2: EntrenateExerciseDemoDefinition = {
  key: "extension-cuadriceps-v2",
  movementFrames: [
    "/exercises/v2/extension-cuadriceps/frame-01.png",
    "/exercises/v2/extension-cuadriceps/frame-02.png",
    "/exercises/v2/extension-cuadriceps/frame-03.png",
    "/exercises/v2/extension-cuadriceps/frame-04.png",
    "/exercises/v2/extension-cuadriceps/frame-05.png",
  ],
};

const zancadasV2: EntrenateExerciseDemoDefinition = {
  key: "zancadas-v2",
  movementFrames: [
    "/exercises/v2/zancadas/frame-01.png",
    "/exercises/v2/zancadas/frame-02.png",
    "/exercises/v2/zancadas/frame-03.png",
    "/exercises/v2/zancadas/frame-04.png",
    "/exercises/v2/zancadas/frame-05.png",
  ],
};

const pesoMuertoRumanoV2: EntrenateExerciseDemoDefinition = {
  key: "peso-muerto-rumano-v2",
  movementFrames: [
    "/exercises/v2/peso-muerto-rumano/frame-01.png",
    "/exercises/v2/peso-muerto-rumano/frame-02.png",
    "/exercises/v2/peso-muerto-rumano/frame-03.png",
    "/exercises/v2/peso-muerto-rumano/frame-04.png",
    "/exercises/v2/peso-muerto-rumano/frame-05.png",
  ],
};

const curlFemoralV2: EntrenateExerciseDemoDefinition = {
  key: "curl-femoral-v2",
  movementFrames: [
    "/exercises/v2/curl-femoral/frame-01.png",
    "/exercises/v2/curl-femoral/frame-02.png",
    "/exercises/v2/curl-femoral/frame-03.png",
    "/exercises/v2/curl-femoral/frame-04.png",
    "/exercises/v2/curl-femoral/frame-05.png",
  ],
};

const hipThrustV2: EntrenateExerciseDemoDefinition = {
  key: "hip-thrust-v2",
  movementFrames: [
    "/exercises/v2/hip-thrust/frame-01.png",
    "/exercises/v2/hip-thrust/frame-02.png",
    "/exercises/v2/hip-thrust/frame-03.png",
    "/exercises/v2/hip-thrust/frame-04.png",
    "/exercises/v2/hip-thrust/frame-05.png",
  ],
};

const puenteGluteosV2: EntrenateExerciseDemoDefinition = {
  key: "puente-gluteos-v2",
  movementFrames: [
    "/exercises/v2/puente-gluteos/frame-01.png",
    "/exercises/v2/puente-gluteos/frame-02.png",
    "/exercises/v2/puente-gluteos/frame-03.png",
    "/exercises/v2/puente-gluteos/frame-04.png",
    "/exercises/v2/puente-gluteos/frame-05.png",
  ],
};

const demoDefinitions = new Map<string, EntrenateExerciseDemoDefinition>([
  ["curl con barra ez", curlBarraEzV2],
  ["press banca con barra", pressBancaBarraV2],
  ["sentadilla con barra", sentadillaBarraV2],
  ["press inclinado con mancuernas", pressInclinadoMancuernasV2],
  ["jalon al pecho", jalonPechoV2],
  ["remo con barra", remoBarraV2],
  ["remo con mancuerna", remoMancuernaV2],
  ["remo en polea baja", remoPoleaBajaV2],
  ["dominadas", dominadasV2],
  ["face pull", facePullV2],
  ["press banca con mancuernas", pressBancaMancuernasV2],
  ["aperturas con mancuernas", aperturaMancuernaV2],
  ["peck deck", peckDeckV2],
  ["flexiones", flexionesV2],
  ["press militar con barra", pressMilitarBarraV2],
  ["press de hombros con mancuernas", pressHombroMancuernasV2],
  ["pike push-up", pikePushUpV2],
  ["elevaciones laterales con mancuernas", elevacionesLateralesMancuernasV2],
  ["curl con mancuernas", curlMancuernasV2],
  ["curl en polea", curlPoleaV2],
  ["extension de triceps en polea", extensionTricepsPoleaV2],
  ["press frances con barra ez", pressFrancesBarraEzV2],
  ["fondos en paralelas", fondosParalelasV2],
  ["sentadilla goblet", sentadillaGobletV2],
  ["sentadilla con peso corporal", sentadillaPesoCorporalV2],
  ["prensa de piernas", prensaV2],
  ["hack squat", hackSquatV2],
  ["extension de cuadriceps", extensionCuadricepsV2],
  ["zancadas con mancuernas", zancadasV2],
  ["peso muerto rumano con barra", pesoMuertoRumanoV2],
  ["curl femoral", curlFemoralV2],
  ["hip thrust con barra", hipThrustV2],
  ["puente de gluteos", puenteGluteosV2],
]);

export function getEntrenateExerciseDemoV2(
  exerciseName: string,
): EntrenateExerciseDemoDefinition | null {
  return demoDefinitions.get(normalizeExerciseName(exerciseName)) ?? null;
}
