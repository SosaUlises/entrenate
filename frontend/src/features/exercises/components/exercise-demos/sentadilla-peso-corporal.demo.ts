import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/sentadilla-peso-corporal";

export const sentadillaPesoCorporalDemo: EntrenateExerciseDemoDefinition = {
  key: "sentadilla-peso-corporal",
  movementFrames: [
    `${basePath}/sentadilla-peso-corporal-01.png`,
    `${basePath}/sentadilla-peso-corporal-02.png`,
    `${basePath}/sentadilla-peso-corporal-03.png`,
    `${basePath}/sentadilla-peso-corporal-04.png`,
  ],
};
