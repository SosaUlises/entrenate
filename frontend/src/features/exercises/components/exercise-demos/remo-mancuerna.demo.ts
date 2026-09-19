import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/remo-mancuerna";

export const remoMancuernaDemo: EntrenateExerciseDemoDefinition = {
  key: "remo-mancuerna",
  movementFrames: [
    `${basePath}/remo-mancuerna-side-01.png`,
    `${basePath}/remo-mancuerna-side-02.png`,
    `${basePath}/remo-mancuerna-side-03.png`,
  ],
};
