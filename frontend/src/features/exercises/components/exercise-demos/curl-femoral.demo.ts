import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/curl-femoral";

export const curlFemoralDemo: EntrenateExerciseDemoDefinition = {
  key: "curl-femoral",
  movementFrames: [
    `${basePath}/curl-femoral-01.png`,
    `${basePath}/curl-femoral-02.png`,
    `${basePath}/curl-femoral-03.png`,
    `${basePath}/curl-femoral-04.png`,
  ],
};
