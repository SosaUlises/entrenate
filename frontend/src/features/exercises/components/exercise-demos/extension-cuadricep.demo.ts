import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/extension-cuadricep";

export const extensionCuadricepDemo: EntrenateExerciseDemoDefinition = {
  key: "extension-cuadricep",
  movementFrames: [
    `${basePath}/extension-cuadricep-01.png`,
    `${basePath}/extension-cuadricep-02.png`,
    `${basePath}/extension-cuadricep-03.png`,
    `${basePath}/extension-cuadricep-04.png`,
  ],
};
