"use client";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import { diasEntrenamientoPorSemanaValues } from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

export function TrainingDaysStep() {
  const { draft, setDiasEntrenamientoPorSemana } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/experience",
    nextPath: "/onboarding/preferred-days",
  });
  const selectedDays = draft.diasEntrenamientoPorSemana;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader
        currentStep={3}
        onBack={goBack}
      />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuántos días por semana querés entrenar?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí una frecuencia que puedas mantener.
        </p>
      </div>

      <fieldset className="mt-6 grid grid-cols-4 gap-3 md:mt-8 md:grid-cols-6">
        <legend className="sr-only">Días de entrenamiento por semana</legend>
        {diasEntrenamientoPorSemanaValues.map((days) => {
          const isSelected = selectedDays === days;
          const dayLabel = days === 1 ? "día" : "días";

          return (
            <label
              className={cn(
                "col-span-2 block cursor-pointer",
                days === 7 && "col-start-2 md:col-start-3",
              )}
              key={days}
            >
              <input
                aria-label={`${days} ${dayLabel} por semana`}
                checked={isSelected}
                className="peer sr-only"
                name="diasEntrenamientoPorSemana"
                onChange={() => setDiasEntrenamientoPorSemana(days)}
                type="radio"
                value={days}
              />
              <span
                className={cn(
                  "flex min-h-20 flex-col items-center justify-center rounded-card border px-3 py-3 transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))]"
                    : "border-border bg-surface",
                )}
              >
                <span
                  className={cn(
                    "font-brand text-3xl leading-none font-bold",
                    isSelected ? "text-primary" : "text-text-primary",
                  )}
                >
                  {days}
                </span>
                <span
                  className={cn(
                    "mt-1.5 text-sm font-semibold",
                    isSelected ? "text-text-primary" : "text-text-secondary",
                  )}
                >
                  {dayLabel}
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <p className="mt-4 text-sm leading-6 font-medium text-text-secondary md:mt-5">
        Después vas a poder elegir qué días preferís.
      </p>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={selectedDays === undefined}
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
