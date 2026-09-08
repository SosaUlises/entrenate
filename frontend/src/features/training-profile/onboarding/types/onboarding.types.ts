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

export const DiaSemana = {
  Domingo: 7,
  Jueves: 4,
  Lunes: 1,
  Martes: 2,
  Miercoles: 3,
  Sabado: 6,
  Viernes: 5,
} as const;

export type DiaSemana = (typeof DiaSemana)[keyof typeof DiaSemana];

export const duracionSesionMinutosValues = [30, 45, 60, 75, 90, 120] as const;

export type DuracionSesionMinutos =
  (typeof duracionSesionMinutosValues)[number];

export const EntornoEntrenamiento = {
  Calistenia: 3,
  Casa: 1,
  GimnasioComercial: 4,
  GimnasioPequeno: 2,
} as const;

export type EntornoEntrenamiento =
  (typeof EntornoEntrenamiento)[keyof typeof EntornoEntrenamiento];

export type OnboardingDraft = {
  diasEntrenamientoPorSemana?: DiasEntrenamientoPorSemana;
  diasPreferidos?: DiaSemana[];
  duracionSesionMinutos?: DuracionSesionMinutos;
  edad?: number;
  equipamientoIds?: string[];
  entornoEntrenamiento?: EntornoEntrenamiento;
  nivelExperiencia?: NivelExperiencia;
  objetivo?: ObjetivoEntrenamiento;
  pesoKg?: number | null;
};
