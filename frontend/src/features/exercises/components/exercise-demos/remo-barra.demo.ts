import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/remo-barra";

export const remoBarraDemo: EntrenateExerciseDemoDefinition = {
  key: "remo-barra",
  movementFrames: [
    `${basePath}/remo-barra-side-01.png`,
    `${basePath}/remo-barra-side-02.png`,
    `${basePath}/remo-barra-side-03.png`,
    `${basePath}/remo-barra-side-04.png`,
  ],
};
