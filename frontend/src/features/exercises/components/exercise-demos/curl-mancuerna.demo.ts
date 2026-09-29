import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/curl-mancuerna";

export const curlMancuernaDemo: EntrenateExerciseDemoDefinition = {
  key: "curl-mancuerna",
  movementFrames: [
    `${basePath}/curl-mancuerna-01.png`,
    `${basePath}/curl-mancuerna-02.png`,
    `${basePath}/curl-mancuerna-03.png`,
    `${basePath}/curl-mancuerna-04.png`,
    `${basePath}/curl-mancuerna-05.png`,
  ],
};
