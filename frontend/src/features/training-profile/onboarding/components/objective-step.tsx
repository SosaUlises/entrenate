"use client";

import {
  BicepsFlexed,
  Check,
  Dumbbell,
  HeartPulse,
  TrendingUp,
  type LucideIcon,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  ObjetivoEntrenamiento,
  type ObjetivoEntrenamiento as ObjetivoEntrenamientoValue,
} from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

const objectiveOptions: ReadonlyArray<
  {
    description: string;
    icon: LucideIcon;
    title: string;
    value: ObjetivoEntrenamientoValue;
  }
> = [
  {
    description: "Aumentar tamaño y desarrollo muscular",
    icon: BicepsFlexed,
    title: "Ganar masa muscular",
    value: ObjetivoEntrenamiento.GanarMasaMuscular,
  },
  {
    description: "Mejorar tu rendimiento y mover más peso",
    icon: Dumbbell,
    title: "Ganar fuerza",
    value: ObjetivoEntrenamiento.GanarFuerza,
  },
  {
    description: "Progresar en ambos objetivos",
    icon: TrendingUp,
    title: "Masa muscular y fuerza",
    value: ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
  },
  {
    description: "Mejorar tu estado físico general",
    icon: HeartPulse,
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

      <fieldset className="mx-auto mt-6 grid w-full max-w-[520px] auto-rows-fr grid-cols-2 gap-3 md:mt-8">
        <legend className="sr-only">Objetivo principal</legend>
        {objectiveOptions.map((option) => {
          const isSelected = selectedObjective === option.value;
          const Icon = option.icon;

          return (
            <label className="block h-full cursor-pointer" key={option.value}>
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
                  "relative flex h-full min-h-[168px] flex-col items-center justify-center rounded-card border px-3 py-4 text-center transition-[background-color,border-color,box-shadow,transform] duration-200 hover:border-border-strong active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-primary/5"
                    : "border-border bg-surface",
                )}
              >
                {isSelected ? (
                  <Check
                    aria-hidden="true"
                    className="absolute top-3 right-3 text-primary"
                    size={16}
                    strokeWidth={2.5}
                  />
                ) : null}

                <span
                  aria-hidden="true"
                  className={cn(
                    "flex size-12 shrink-0 items-center justify-center rounded-control bg-surface-elevated transition-colors",
                    isSelected
                      ? "bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface-elevated))] text-primary"
                      : "text-text-secondary",
                  )}
                >
                  <Icon size={34} strokeWidth={2} />
                </span>

                <span className="mt-3 text-[15px] leading-5 font-semibold text-text-primary sm:text-base">
                  {option.title}
                </span>
                <span className="mt-1 text-xs leading-[18px] text-text-secondary sm:text-[13px]">
                  {option.description}
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <div className="mx-auto mt-6 w-full max-w-[520px]">
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
