import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/sentadilla-barra";

export const sentadillaBarraDemo: EntrenateExerciseDemoDefinition = {
  key: "sentadilla-barra",
  movementFrames: [
    `${basePath}/sentadilla-barra-01.png`,
    `${basePath}/sentadilla-barra-02.png`,
    `${basePath}/sentadilla-barra-03.png`,
    `${basePath}/sentadilla-barra-04.png`,
  ],
};
