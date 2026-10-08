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
export {
  environmentLabels,
  experienceLabels,
  formatPreferredDays,
  formatWeight,
  objectiveLabels,
  sexLabels,
} from "../shared/training-profile-options";

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
