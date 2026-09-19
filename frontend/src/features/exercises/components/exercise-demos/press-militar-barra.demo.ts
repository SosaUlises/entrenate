import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/press-militar-barra";

export const pressMilitarBarraDemo: EntrenateExerciseDemoDefinition = {
  key: "press-militar-barra",
  movementFrames: [
    `${basePath}/press-militar-barra-side-01.png`,
    `${basePath}/press-militar-barra-side-02.png`,
    `${basePath}/press-militar-barra-side-03.png`,
    `${basePath}/press-militar-barra-side-04.png`,
  ],
};
