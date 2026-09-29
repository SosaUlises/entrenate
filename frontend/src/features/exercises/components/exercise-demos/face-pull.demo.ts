import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/face-pull";

export const facePullDemo: EntrenateExerciseDemoDefinition = {
  key: "face-pull",
  movementFrames: [
    `${basePath}/face-pull-01.png`,
    `${basePath}/face-pull-02.png`,
    `${basePath}/face-pull-03.png`,
    `${basePath}/face-pull-04.png`,
    `${basePath}/face-pull-05.png`,
  ],
};
