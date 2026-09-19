import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/press-banca-mancuerna";

export const pressBancaMancuernasDemo: EntrenateExerciseDemoDefinition = {
  key: "press-banca-mancuernas",
  movementFrames: [
    `${basePath}/press-banca-mancuerna-side-01.png`,
    `${basePath}/press-banca-mancuerna-side-02.png`,
    `${basePath}/press-banca-mancuerna-side-03.png`,
    `${basePath}/press-banca-mancuerna-side-04.png`,
  ],
  armPositionFrames: [
    `${basePath}/press-banca-mancuerna-top-01.png`,
    `${basePath}/press-banca-mancuerna-top-02.png`,
  ],
};
