"use client";

import { Check } from "lucide-react";
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
      <OnboardingStepHeader currentStep={3} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuántos días por semana querés entrenar?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí una frecuencia que puedas mantener.
        </p>
      </div>

      <fieldset className="mt-6 grid grid-cols-4 gap-3.5 md:mt-8 md:grid-cols-6">
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
                  "relative flex h-[5.25rem] flex-col items-center justify-center rounded-card border px-3 py-3 transition-[background-color,border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-thermal-accent/40 bg-surface-active shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                    : "border-border bg-surface-interactive hover:border-border-strong",
                )}
              >
                <span
                  className="font-brand text-[2rem] font-extrabold leading-none tracking-[-0.03em] text-text-primary"
                >
                  {days}
                </span>
                <span className="mt-1 text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                  {dayLabel}
                </span>

                <span
                  aria-hidden="true"
                  className={cn(
                    "absolute right-2.5 top-2.5 flex size-5 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
                    isSelected
                      ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                      : "scale-95 border-border-strong/70 bg-surface text-transparent",
                  )}
                >
                  <Check
                    className={cn(
                      "transition-transform duration-200",
                      isSelected ? "scale-100" : "scale-0",
                    )}
                    size={11}
                    strokeWidth={2.25}
                  />
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <p className="mt-3.5 text-[0.8125rem] font-medium leading-5 text-text-muted md:mt-4">
        Después vas a poder elegir qué días preferís.
      </p>

      <div className="mt-6 md:mt-7">
        <Button
          disabled={selectedDays === undefined}
          fullWidth
          onClick={goNext}
          size="lg"
          type="button"
          variant="primary"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}
