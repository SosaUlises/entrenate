export const ObjetivoEntrenamiento = {
  AcondicionamientoGeneral: 4,
  GanarFuerza: 2,
  GanarMasaMuscular: 1,
  GanarmasaMuscularYFuerza: 3,
} as const;

export type ObjetivoEntrenamiento =
  (typeof ObjetivoEntrenamiento)[keyof typeof ObjetivoEntrenamiento];

export const NivelExperiencia = {
  Avanzado: 4,
  Intermedio: 3,
  Principiante: 2,
  SinExperiencia: 1,
} as const;

export type NivelExperiencia =
  (typeof NivelExperiencia)[keyof typeof NivelExperiencia];

export const diasEntrenamientoPorSemanaValues = [1, 2, 3, 4, 5, 6, 7] as const;

export type DiasEntrenamientoPorSemana =
  (typeof diasEntrenamientoPorSemanaValues)[number];

export type OnboardingDraft = {
  diasEntrenamientoPorSemana?: DiasEntrenamientoPorSemana;
  nivelExperiencia?: NivelExperiencia;
  objetivo?: ObjetivoEntrenamiento;
};
