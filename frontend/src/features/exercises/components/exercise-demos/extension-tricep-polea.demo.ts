import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/extension-tricep-polea";

export const extensionTricepPoleaDemo: EntrenateExerciseDemoDefinition = {
  key: "extension-tricep-polea",
  movementFrames: [
    `${basePath}/extension-tricep-polea-01.png`,
    `${basePath}/extension-tricep-polea-02.png`,
    `${basePath}/extension-tricep-polea-03.png`,
    `${basePath}/extension-tricep-polea-04.png`,
  ],
};
