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
