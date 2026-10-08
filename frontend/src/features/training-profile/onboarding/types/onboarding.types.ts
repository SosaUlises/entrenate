import type {
  DiaSemana,
  DiasEntrenamientoPorSemana,
  DuracionSesionMinutos,
  EntornoEntrenamiento,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  Sexo,
} from "../../shared/training-profile.types";

export {
  DiaSemana,
  diasEntrenamientoPorSemanaValues,
  duracionSesionMinutosValues,
  EntornoEntrenamiento,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  Sexo,
} from "../../shared/training-profile.types";
export type {
  DiasEntrenamientoPorSemana,
  DuracionSesionMinutos,
} from "../../shared/training-profile.types";

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
  sexo?: Sexo;
};
