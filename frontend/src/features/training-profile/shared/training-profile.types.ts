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

export const Sexo = {
  Femenino: 2,
  Masculino: 1,
  PrefieroNoInformarlo: 3,
} as const;

export type Sexo = (typeof Sexo)[keyof typeof Sexo];

export const CategoriaEquipamiento = {
  PesasLibres: 1,
  Barras: 2,
  BancosYRacks: 3,
  Poleas: 4,
  Maquinas: 5,
  Calistenia: 6,
  Otros: 7,
} as const;

export type CategoriaEquipamiento =
  (typeof CategoriaEquipamiento)[keyof typeof CategoriaEquipamiento];

export type Equipment = {
  categoria: CategoriaEquipamiento;
  id: string;
  nombre: string;
};

export type TrainingProfileEquipment = Equipment;

export type TrainingProfileResponse = {
  actualizadoEnUtc: string;
  creadoEnUtc: string;
  diasEntrenamientoPorSemana: number;
  diasPreferidos: DiaSemana[];
  duracionSesionMinutos: number;
  edad: number;
  entornoEntrenamiento: EntornoEntrenamiento;
  equipamientos: TrainingProfileEquipment[];
  id: string;
  nivelExperiencia: NivelExperiencia;
  objetivo: ObjetivoEntrenamiento;
  pesoKg: number | null;
  sexo: Sexo | null;
};

export type UpdateTrainingProfileRequest = {
  diasEntrenamientoPorSemana: number;
  diasPreferidos: DiaSemana[];
  duracionSesionMinutos: number;
  edad: number;
  entornoEntrenamiento: EntornoEntrenamiento;
  equipamientoIds: string[];
  nivelExperiencia: NivelExperiencia;
  objetivo: ObjetivoEntrenamiento;
  pesoKg: number | null;
  sexo: Sexo | null;
};
