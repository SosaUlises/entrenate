import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/hack-squat";

export const hackSquatDemo: EntrenateExerciseDemoDefinition = {
  key: "hack-squat",
  movementFrames: [
    `${basePath}/hack-squat-01.png`,
    `${basePath}/hack-squat-02.png`,
    `${basePath}/hack-squat-03.png`,
  ],
};
