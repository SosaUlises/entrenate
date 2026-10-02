import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/hip-thrust-barra";

export const hipThrustBarraDemo: EntrenateExerciseDemoDefinition = {
  key: "hip-thrust-barra",
  movementFrames: [
    `${basePath}/hip-thrust-barra-01.png`,
    `${basePath}/hip-thrust-barra-02.png`,
    `${basePath}/hip-thrust-barra-03.png`,
  ],
};
