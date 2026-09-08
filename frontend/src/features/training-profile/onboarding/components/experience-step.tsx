"use client";

import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  NivelExperiencia,
  type NivelExperiencia as NivelExperienciaValue,
} from "../types/onboarding.types";
import {
  OnboardingChoiceStep,
  type OnboardingChoiceOption,
} from "./onboarding-choice-step";
import { ExperienceLevelIndicator } from "./experience-level-indicator";

const experienceOptions: ReadonlyArray<
  OnboardingChoiceOption<NivelExperienciaValue>
> = [
  {
    description: "Estoy empezando desde cero",
    icon: <ExperienceLevelIndicator activeBars={0} />,
    title: "Sin experiencia",
    value: NivelExperiencia.SinExperiencia,
  },
  {
    description: "Tengo algo de experiencia y sigo aprendiendo",
    icon: <ExperienceLevelIndicator activeBars={1} />,
    title: "Principiante",
    value: NivelExperiencia.Principiante,
  },
  {
    description: "Entreno con regularidad y manejo los ejercicios básicos",
    icon: <ExperienceLevelIndicator activeBars={2} />,
    title: "Intermedio",
    value: NivelExperiencia.Intermedio,
  },
  {
    description:
      "Entreno hace tiempo y conozco bien la técnica y la progresión",
    icon: <ExperienceLevelIndicator activeBars={3} />,
    title: "Avanzado",
    value: NivelExperiencia.Avanzado,
  },
];

export function ExperienceStep() {
  const { draft, setNivelExperiencia } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/objective",
    nextPath: "/onboarding/training-days",
  });

  return (
    <OnboardingChoiceStep
      currentStep={2}
      legend="Nivel de experiencia"
      name="nivelExperiencia"
      onBack={goBack}
      onChange={setNivelExperiencia}
      onContinue={goNext}
      options={experienceOptions}
      question="¿Cuánta experiencia tenés entrenando?"
      selectedValue={draft.nivelExperiencia}
      supportingText="Elegí la opción que mejor te represente."
    />
  );
}
