import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/pike-pushup";

export const pikePushupDemo: EntrenateExerciseDemoDefinition = {
  key: "pike-pushup",
  movementFrames: [
    `${basePath}/pike-pushup-01.png`,
    `${basePath}/pike-pushup-02.png`,
    `${basePath}/pike-pushup-03.png`,
    `${basePath}/pike-pushup-04.png`,
    `${basePath}/pike-pushup-05.png`,
  ],
};
