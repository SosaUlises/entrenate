"use client";

import { useState, type ChangeEvent } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import { OnboardingStepHeader } from "./onboarding-step-header";

export function WeightStep() {
  const { draft, setPesoKg } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/age",
    nextPath: "/onboarding/sex",
  });
  const [inputValue, setInputValue] = useState(
    typeof draft.pesoKg === "number" ? draft.pesoKg.toString() : "",
  );
  const [hasBlurred, setHasBlurred] = useState(false);
  const validWeight = parseWeight(inputValue);
  const showError =
    hasBlurred && inputValue.length > 0 && validWeight === undefined;

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const nextValue = event.target.value;
    const nextWeight = parseWeight(nextValue);

    setInputValue(nextValue);
    setHasBlurred(false);
    setPesoKg(nextWeight);
  };

  const normalizeInput = () => {
    if (validWeight !== undefined) {
      setInputValue(validWeight.toString());
    }
  };

  const handleBlur = () => {
    setHasBlurred(true);
    normalizeInput();
  };

  const handleContinue = () => {
    if (validWeight === undefined) {
      return;
    }

    setPesoKg(validWeight);
    normalizeInput();
    goNext();
  };

  const handleSkip = () => {
    setInputValue("");
    setHasBlurred(false);
    setPesoKg(null);
    goNext();
  };

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader
        currentStep={9}
        onBack={goBack}
      />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuánto pesás actualmente?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Esto nos ayuda a personalizar mejor tu entrenamiento.
        </p>
      </div>

      <div className="mt-10 flex flex-col items-center md:mt-12">
        <div
          className={cn(
            "flex h-28 w-44 items-center justify-center rounded-card border bg-surface px-4 transition-[background-color,border-color,box-shadow] duration-200 focus-within:border-primary focus-within:bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))] focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-2 focus-within:ring-offset-background",
            showError && "border-error",
          )}
        >
          <label className="sr-only" htmlFor="peso-kg">
            Peso actual en kilogramos
          </label>
          <input
            aria-describedby={
              showError ? "weight-unit weight-error" : "weight-unit"
            }
            aria-invalid={showError}
            autoComplete="off"
            className="w-full bg-transparent text-center text-text-primary outline-none placeholder:text-text-secondary"
            id="peso-kg"
            inputMode="decimal"
            onBlur={handleBlur}
            onChange={handleChange}
            placeholder="—"
            style={{
              fontFamily: "var(--font-raleway), sans-serif",
              fontSize: "clamp(3rem, 4vw, 3.5rem)",
              fontWeight: 700,
              lineHeight: 1,
            }}
            type="text"
            value={inputValue}
          />
        </div>

        <p
          className="mt-2 text-base leading-6 font-semibold text-text-secondary"
          id="weight-unit"
        >
          kg
        </p>

        <div className="mt-2 min-h-5" aria-live="polite">
          {showError ? (
            <p className="text-sm leading-5 text-error" id="weight-error">
              Ingresá un peso válido mayor a 0.
            </p>
          ) : null}
        </div>
      </div>

      <div className="mt-5 flex flex-col items-center gap-3">
        <Button
          className="min-h-12 px-4"
          onClick={handleSkip}
          type="button"
          variant="text"
        >
          Omitir por ahora
        </Button>
        <Button
          className="min-h-14"
          disabled={validWeight === undefined}
          fullWidth
          onClick={handleContinue}
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}

function parseWeight(value: string): number | undefined {
  if (!/^\d+(?:[.,]\d+)?$/.test(value)) {
    return undefined;
  }

  const weight = Number(value.replace(",", "."));

  if (!Number.isFinite(weight) || weight <= 0) {
    return undefined;
  }

  return weight;
}
