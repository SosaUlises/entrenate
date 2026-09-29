import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/curl-polea";

export const curlPoleaDemo: EntrenateExerciseDemoDefinition = {
  key: "curl-polea",
  movementFrames: [
    `${basePath}/curl-polea-01.png`,
    `${basePath}/curl-polea-02.png`,
    `${basePath}/curl-polea-03.png`,
    `${basePath}/curl-polea-04.png`,
  ],
};
