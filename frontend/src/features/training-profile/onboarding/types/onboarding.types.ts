export const ObjetivoEntrenamiento = {
  AcondicionamientoGeneral: 4,
  GanarFuerza: 2,
  GanarMasaMuscular: 1,
  GanarmasaMuscularYFuerza: 3,
} as const;

export type ObjetivoEntrenamiento =
  (typeof ObjetivoEntrenamiento)[keyof typeof ObjetivoEntrenamiento];

export type OnboardingDraft = {
  objetivo?: ObjetivoEntrenamiento;
};
