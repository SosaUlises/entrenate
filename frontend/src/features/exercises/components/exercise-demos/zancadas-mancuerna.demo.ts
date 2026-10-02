import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/zancadas-mancuerna";

export const zancadasMancuernaDemo: EntrenateExerciseDemoDefinition = {
  key: "zancadas-mancuerna",
  movementFrames: [
    `${basePath}/zancadas-mancuerna-01.png`,
    `${basePath}/zancadas-mancuerna-02.png`,
    `${basePath}/zancadas-mancuerna-03.png`,
    `${basePath}/zancadas-mancuerna-04.png`,
  ],
};
