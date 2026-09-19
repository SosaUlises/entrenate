import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/jalon-pecho";

export const jalonPechoDemo: EntrenateExerciseDemoDefinition = {
  key: "jalon-pecho",
  movementFrames: [
    `${basePath}/jalon-pecho-side-01.png`,
    `${basePath}/jalon-pecho-side-02.png`,
    `${basePath}/jalon-pecho-side-03.png`,
    `${basePath}/jalon-pecho-side-04.png`,
  ],
};
