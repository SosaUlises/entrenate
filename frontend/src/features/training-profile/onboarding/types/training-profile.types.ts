import type {
  DiaSemana,
  EntornoEntrenamiento,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  Sexo,
} from "./onboarding.types";

export type CreateTrainingProfileRequest = {
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

export type CreateTrainingProfileResponse = {
  id: string;
};

export type TrainingProfileEquipment = {
  categoria: number;
  id: string;
  nombre: string;
};

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
