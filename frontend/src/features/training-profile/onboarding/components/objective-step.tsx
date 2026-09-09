"use client";

import type { ComponentType } from "react";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  ObjetivoEntrenamiento,
  type ObjetivoEntrenamiento as ObjetivoEntrenamientoValue,
} from "../types/onboarding.types";
import {
  GeneralConditioningIllustration,
  MuscleAndStrengthIllustration,
  MuscleGainIllustration,
  StrengthIllustration,
  type ObjectiveIllustrationProps,
} from "./objective-illustrations";
import { OnboardingStepHeader } from "./onboarding-step-header";

const objectiveOptions: ReadonlyArray<
  {
    description: string;
    illustration: ComponentType<ObjectiveIllustrationProps>;
    title: string;
    value: ObjetivoEntrenamientoValue;
  }
> = [
  {
    description: "Aumentar tamaño y desarrollo muscular",
    illustration: MuscleGainIllustration,
    title: "Ganar masa muscular",
    value: ObjetivoEntrenamiento.GanarMasaMuscular,
  },
  {
    description: "Mejorar tu rendimiento y mover más peso",
    illustration: StrengthIllustration,
    title: "Ganar fuerza",
    value: ObjetivoEntrenamiento.GanarFuerza,
  },
  {
    description: "Progresar en ambos objetivos",
    illustration: MuscleAndStrengthIllustration,
    title: "Masa muscular y fuerza",
    value: ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
  },
  {
    description: "Mejorar tu estado físico general",
    illustration: GeneralConditioningIllustration,
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
  const selectedObjective = draft.objetivo;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={1} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuál es tu objetivo principal?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí lo que más querés priorizar.
        </p>
      </div>

      <fieldset className="mx-auto mt-6 grid w-full max-w-[520px] auto-rows-[180px] grid-cols-2 gap-3 md:mt-8">
        <legend className="sr-only">Objetivo principal</legend>
        {objectiveOptions.map((option) => {
          const isSelected = selectedObjective === option.value;
          const Illustration = option.illustration;

          return (
            <label
              className="block h-full min-w-0 cursor-pointer"
              key={option.value}
            >
              <input
                checked={isSelected}
                className="peer sr-only"
                name="objetivo"
                onChange={() => setObjetivo(option.value)}
                type="radio"
                value={option.value}
              />
              <span
                className={cn(
                  "relative flex h-full w-full min-w-0 flex-col items-center justify-center rounded-card border px-3 py-3 text-center transition-[background-color,border-color,box-shadow,transform] duration-200 hover:border-border-strong active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))]"
                    : "border-border bg-surface",
                )}
              >
                {isSelected ? (
                  <span className="absolute top-3 right-3 flex size-[22px] items-center justify-center rounded-full border border-primary/30 bg-primary/10 text-primary">
                    <Check
                      aria-hidden="true"
                      size={13}
                      strokeWidth={2.5}
                    />
                  </span>
                ) : null}

                <span
                  aria-hidden="true"
                  className={cn(
                    "flex h-16 w-full shrink-0 items-center justify-center transition-colors",
                    isSelected
                      ? "text-[color-mix(in_srgb,var(--primary)_72%,var(--text-secondary))]"
                      : "text-text-secondary",
                  )}
                >
                  <Illustration isSelected={isSelected} />
                </span>

                <span className="mt-3 flex min-h-10 items-center justify-center text-[15px] leading-5 font-semibold text-text-primary sm:text-base">
                  {option.title}
                </span>
                <span className="mt-1.5 flex min-h-9 max-w-36 items-start justify-center text-[13px] leading-[18px] text-text-secondary">
                  {option.description}
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <div className="mx-auto mt-7 w-full max-w-[520px]">
        <Button
          className="min-h-14"
          disabled={selectedObjective === undefined}
          fullWidth
          onClick={goNext}
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}
