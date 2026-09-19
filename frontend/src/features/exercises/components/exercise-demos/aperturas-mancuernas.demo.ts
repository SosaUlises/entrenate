import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/apertura-mancuerna";

export const aperturasMancuernasDemo: EntrenateExerciseDemoDefinition = {
  key: "aperturas-mancuernas",
  movementFrames: [
    `${basePath}/apertura-mancuernas-side-01.png`,
    `${basePath}/apertura-mancuernas-side-02.png`,
    `${basePath}/apertura-mancuernas-side-03.png`,
    `${basePath}/apertura-mancuernas-side-04.png`,
  ],
  armPositionFrames: [
    `${basePath}/apertura-mancuernas-top-01.png`,
    `${basePath}/apertura-mancuernas-top-02.png`,
    `${basePath}/apertura-mancuernas-top-03.png`,
    `${basePath}/apertura-mancuernas-top-04.png`,
  ],
};
