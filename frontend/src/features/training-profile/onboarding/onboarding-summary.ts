import {
  DiaSemana,
  diasEntrenamientoPorSemanaValues,
  duracionSesionMinutosValues,
  EntornoEntrenamiento,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  Sexo,
  type OnboardingDraft,
} from "./types/onboarding.types";
import type { CreateTrainingProfileRequest } from "./types/training-profile.types";

export const objectiveLabels: Record<ObjetivoEntrenamiento, string> = {
  [ObjetivoEntrenamiento.GanarMasaMuscular]: "Ganar masa muscular",
  [ObjetivoEntrenamiento.GanarFuerza]: "Ganar fuerza",
  [ObjetivoEntrenamiento.GanarmasaMuscularYFuerza]:
    "Masa muscular y fuerza",
  [ObjetivoEntrenamiento.AcondicionamientoGeneral]:
    "Acondicionamiento general",
};

export const experienceLabels: Record<NivelExperiencia, string> = {
  [NivelExperiencia.SinExperiencia]: "Sin experiencia",
  [NivelExperiencia.Principiante]: "Principiante",
  [NivelExperiencia.Intermedio]: "Intermedio",
  [NivelExperiencia.Avanzado]: "Avanzado",
};

export const environmentLabels: Record<EntornoEntrenamiento, string> = {
  [EntornoEntrenamiento.Casa]: "Casa",
  [EntornoEntrenamiento.GimnasioPequeno]: "Gimnasio pequeño",
  [EntornoEntrenamiento.Calistenia]: "Calistenia",
  [EntornoEntrenamiento.GimnasioComercial]: "Gimnasio comercial",
};

export const sexLabels: Record<Sexo, string> = {
  [Sexo.Masculino]: "Masculino",
  [Sexo.Femenino]: "Femenino",
  [Sexo.PrefieroNoInformarlo]: "Prefiero no informarlo",
};

const dayLabels: Record<DiaSemana, string> = {
  [DiaSemana.Lunes]: "Lun",
  [DiaSemana.Martes]: "Mar",
  [DiaSemana.Miercoles]: "Mié",
  [DiaSemana.Jueves]: "Jue",
  [DiaSemana.Viernes]: "Vie",
  [DiaSemana.Sabado]: "Sáb",
  [DiaSemana.Domingo]: "Dom",
};

const objectiveValues = Object.values(ObjetivoEntrenamiento);
const experienceValues = Object.values(NivelExperiencia);
const environmentValues = Object.values(EntornoEntrenamiento);
const dayValues = Object.values(DiaSemana);
const sexValues = Object.values(Sexo);

export function getFirstIncompleteStepPath(
  draft: OnboardingDraft,
): string | undefined {
  if (!objectiveValues.includes(draft.objetivo as ObjetivoEntrenamiento)) {
    return "/onboarding/objective";
  }

  if (!experienceValues.includes(draft.nivelExperiencia as NivelExperiencia)) {
    return "/onboarding/experience";
  }

  if (
    !diasEntrenamientoPorSemanaValues.includes(
      draft.diasEntrenamientoPorSemana as (typeof diasEntrenamientoPorSemanaValues)[number],
    )
  ) {
    return "/onboarding/training-days";
  }

  if (
    !isValidPreferredDays(
      draft.diasPreferidos,
      draft.diasEntrenamientoPorSemana,
    )
  ) {
    return "/onboarding/preferred-days";
  }

  if (
    !duracionSesionMinutosValues.includes(
      draft.duracionSesionMinutos as (typeof duracionSesionMinutosValues)[number],
    )
  ) {
    return "/onboarding/duration";
  }

  if (
    !environmentValues.includes(
      draft.entornoEntrenamiento as EntornoEntrenamiento,
    )
  ) {
    return "/onboarding/environment";
  }

  if (!isValidEquipmentIds(draft.equipamientoIds)) {
    return "/onboarding/equipment";
  }

  if (
    !Number.isInteger(draft.edad) ||
    draft.edad === undefined ||
    draft.edad < 1 ||
    draft.edad > 120
  ) {
    return "/onboarding/age";
  }

  if (
    draft.pesoKg === undefined ||
    (draft.pesoKg !== null &&
      (!Number.isFinite(draft.pesoKg) || draft.pesoKg <= 0))
  ) {
    return "/onboarding/weight";
  }

  if (!sexValues.includes(draft.sexo as Sexo)) {
    return "/onboarding/sex";
  }

  return undefined;
}

export function createTrainingProfileRequestFromDraft(
  draft: OnboardingDraft,
): CreateTrainingProfileRequest | undefined {
  if (getFirstIncompleteStepPath(draft)) {
    return undefined;
  }

  return {
    diasEntrenamientoPorSemana: draft.diasEntrenamientoPorSemana!,
    diasPreferidos: [...draft.diasPreferidos!],
    duracionSesionMinutos: draft.duracionSesionMinutos!,
    edad: draft.edad!,
    entornoEntrenamiento: draft.entornoEntrenamiento!,
    equipamientoIds: [...draft.equipamientoIds!],
    nivelExperiencia: draft.nivelExperiencia!,
    objetivo: draft.objetivo!,
    pesoKg: draft.pesoKg!,
    sexo: draft.sexo!,
  };
}

export function formatPreferredDays(days: DiaSemana[]): string {
  return days.length === 0
    ? "Sin días fijos"
    : [...days]
        .sort((firstDay, secondDay) => firstDay - secondDay)
        .map((day) => dayLabels[day])
        .join(" · ");
}

export function formatWeight(weight: number | null): string {
  return weight === null
    ? "No informado"
    : `${new Intl.NumberFormat("es-AR", {
        maximumFractionDigits: 2,
      }).format(weight)} kg`;
}

function isValidPreferredDays(
  days: DiaSemana[] | undefined,
  maximumDays: number | undefined,
): days is DiaSemana[] {
  return (
    days !== undefined &&
    maximumDays !== undefined &&
    days.length <= maximumDays &&
    new Set(days).size === days.length &&
    days.every((day) => dayValues.includes(day))
  );
}

function isValidEquipmentIds(ids: string[] | undefined): ids is string[] {
  return (
    ids !== undefined &&
    new Set(ids).size === ids.length &&
    ids.every((id) => id.trim().length > 0)
  );
}
