import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/flexiones";

export const flexionesDemo: EntrenateExerciseDemoDefinition = {
  key: "flexiones",
  movementFrames: [
    `${basePath}/flexiones-side-01.png`,
    `${basePath}/flexiones-side-02.png`,
    `${basePath}/flexiones-side-03.png`,
    `${basePath}/flexiones-side-04.png`,
  ],
};
