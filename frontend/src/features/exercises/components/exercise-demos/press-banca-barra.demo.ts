import type { EntrenateExerciseDemoDefinition } from "../exercise-demo-registry";

const basePath = "/exercise-demos/press-banca-barra";

export const pressBancaBarraDemo: EntrenateExerciseDemoDefinition = {
  key: "press-banca-barra",
  movementFrames: [
    `${basePath}/press-banca-barra-side-01.png`,
    `${basePath}/press-banca-barra-side-02.png`,
    `${basePath}/press-banca-barra-side-03.png`,
  ],
  armPositionFrames: [
    `${basePath}/press-banca-barra-top-01.png`,
    `${basePath}/press-banca-barra-top-02.png`,
  ],
};
