"use client";

import { Check } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { duracionSesionMinutosValues } from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

export function DurationStep() {
  const router = useRouter();
  const { draft, setDuracionSesionMinutos } = useOnboarding();
  const selectedDuration = draft.duracionSesionMinutos;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader
        currentStep={5}
        onBack={() => router.push("/onboarding/preferred-days")}
      />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuánto tiempo tenés normalmente para entrenar?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí una duración aproximada.
        </p>
      </div>

      <fieldset className="mt-6 grid grid-cols-2 gap-3 md:mt-8 md:grid-cols-3">
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
                  "relative flex min-h-24 flex-col items-center justify-center rounded-card border px-3 py-3 transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))]"
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
                  className={cn(
                    "font-brand text-3xl leading-none font-bold sm:text-[2rem]",
                    isSelected ? "text-primary" : "text-text-primary",
                  )}
                >
                  {duration}
                </span>
                <span
                  className={cn(
                    "mt-1.5 text-sm font-semibold",
                    isSelected ? "text-text-primary" : "text-text-secondary",
                  )}
                >
                  min
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={selectedDuration === undefined}
          fullWidth
          onClick={() => router.push("/onboarding/environment")}
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}
