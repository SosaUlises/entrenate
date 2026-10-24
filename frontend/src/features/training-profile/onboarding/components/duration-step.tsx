"use client";

import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import { duracionSesionMinutosValues } from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

export function DurationStep() {
  const { draft, setDuracionSesionMinutos } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/preferred-days",
    nextPath: "/onboarding/environment",
  });
  const selectedDuration = draft.duracionSesionMinutos;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={5} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuánto tiempo tenés normalmente para entrenar?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí una duración aproximada.
        </p>
      </div>

      <fieldset className="mt-6 grid grid-cols-2 gap-3.5 md:mt-8">
        <legend className="sr-only">Duración del entrenamiento</legend>
        {duracionSesionMinutosValues.map((duration) => {
          const isSelected = selectedDuration === duration;

          return (
            <label className="block cursor-pointer" key={duration}>
              <input
                aria-label={`${duration} minutos`}
                checked={isSelected}
                className="peer sr-only"
                name="duracionSesionMinutos"
                onChange={() => setDuracionSesionMinutos(duration)}
                type="radio"
                value={duration}
              />
              <span
                className={cn(
                  "relative flex h-[5.25rem] flex-col items-center justify-center rounded-card border px-3 py-3 transition-[background-color,border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-thermal-accent/40 bg-surface-active shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                    : "border-border bg-surface-interactive hover:border-border-strong",
                )}
              >
                <span className="font-brand text-[2rem] font-extrabold leading-none tracking-[-0.03em] text-text-primary">
                  {duration}
                </span>
                <span className="mt-1 text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                  min
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

      <div className="mt-6 md:mt-7">
        <Button
          disabled={selectedDuration === undefined}
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
