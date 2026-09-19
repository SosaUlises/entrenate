import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/press-inclinado-mancuerna";

export const pressInclinadoMancuernasDemo: EntrenateExerciseDemoDefinition = {
  key: "press-inclinado-mancuernas",
  movementFrames: [
    `${basePath}/press-inclinado-mancuerna-side-01.png`,
    `${basePath}/press-inclinado-mancuerna-side-02.png`,
    `${basePath}/press-inclinado-mancuerna-side-03.png`,
  ],
  armPositionFrames: [
    `${basePath}/press-inclinado-mancuerna-top-01.png`,
    `${basePath}/press-inclinado-mancuerna-top-02.png`,
  ],
};
