import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/prensa-pierna";

export const prensaPiernaDemo: EntrenateExerciseDemoDefinition = {
  key: "prensa-pierna",
  movementFrames: [
    `${basePath}/prensa-pierna-01.png`,
    `${basePath}/prensa-pierna-02.png`,
    `${basePath}/prensa-pierna-03.png`,
  ],
};
