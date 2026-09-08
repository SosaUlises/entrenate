"use client";

import { BicepsFlexed, Dumbbell, HeartPulse, TrendingUp } from "lucide-react";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  ObjetivoEntrenamiento,
  type ObjetivoEntrenamiento as ObjetivoEntrenamientoValue,
} from "../types/onboarding.types";
import {
  OnboardingChoiceStep,
  type OnboardingChoiceOption,
} from "./onboarding-choice-step";

const objectiveOptions: ReadonlyArray<
  OnboardingChoiceOption<ObjetivoEntrenamientoValue>
> = [
  {
    description: "Aumentar tamaño y desarrollo muscular",
    icon: <BicepsFlexed size={24} strokeWidth={2} />,
    title: "Ganar masa muscular",
    value: ObjetivoEntrenamiento.GanarMasaMuscular,
  },
  {
    description: "Mejorar tu rendimiento y mover más peso",
    icon: <Dumbbell size={24} strokeWidth={2} />,
    title: "Ganar fuerza",
    value: ObjetivoEntrenamiento.GanarFuerza,
  },
  {
    description: "Progresar en ambos objetivos",
    icon: <TrendingUp size={24} strokeWidth={2} />,
    title: "Masa muscular y fuerza",
    value: ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
  },
  {
    description: "Mejorar tu estado físico general",
    icon: <HeartPulse size={24} strokeWidth={2} />,
    title: "Acondicionamiento general",
    value: ObjetivoEntrenamiento.AcondicionamientoGeneral,
  },
];

export function ObjectiveStep() {
  const { draft, setObjetivo } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding",
    nextPath: "/onboarding/experience",
  });

  return (
    <OnboardingChoiceStep
      currentStep={1}
      legend="Objetivo principal"
      name="objetivo"
      onBack={goBack}
      onChange={setObjetivo}
      onContinue={goNext}
      options={objectiveOptions}
      question="¿Cuál es tu objetivo principal?"
      selectedValue={draft.objetivo}
      supportingText="Elegí lo que más querés priorizar."
    />
  );
}
