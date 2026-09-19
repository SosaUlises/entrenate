import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/peck-deck";

export const peckDeckDemo: EntrenateExerciseDemoDefinition = {
  key: "peck-deck",
  movementFrames: [
    `${basePath}/peck-deck-front-01.png`,
    `${basePath}/peck-deck-front-02.png`,
    `${basePath}/peck-deck-front-03.png`,
    `${basePath}/peck-deck-front-04.png`,
    `${basePath}/peck-deck-front-05.png`,
  ],
};
