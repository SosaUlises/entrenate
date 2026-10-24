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
      <OnboardingStepHeader currentStep={9} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuánto pesás actualmente?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Esto nos ayuda a personalizar mejor tu entrenamiento.
        </p>
      </div>

      <div className="mt-6 flex flex-col items-center md:mt-8">
        <label
          className={cn(
            "flex h-28 w-full max-w-[20rem] cursor-text flex-col items-center justify-center rounded-card border border-border bg-surface-interactive px-5 py-3 shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] transition-[background-color,border-color,box-shadow] duration-200 focus-within:border-thermal-accent/45 focus-within:bg-surface-active focus-within:ring-2 focus-within:ring-thermal-accent focus-within:ring-offset-2 focus-within:ring-offset-background",
            showError && "border-error/70",
          )}
          htmlFor="peso-kg"
        >
          <span className="sr-only">
            Peso actual en kilogramos
          </span>
          <input
            aria-describedby={
              showError ? "weight-unit weight-error" : "weight-unit"
            }
            aria-invalid={showError}
            autoComplete="off"
            className="w-full max-w-52 border-0 bg-transparent text-center font-brand font-extrabold tracking-[-0.04em] text-text-primary caret-thermal-accent shadow-none outline-none placeholder:text-text-muted focus:border-0 focus:outline-none focus:ring-0 focus-visible:outline-none focus-visible:ring-0 focus-visible:shadow-none"
            id="peso-kg"
            inputMode="decimal"
            onBlur={handleBlur}
            onChange={handleChange}
            placeholder="—"
            style={{
              border: 0,
              boxShadow: "none",
              fontFamily: "var(--font-raleway), sans-serif",
              fontSize: "clamp(3rem, 9vw, 3.5rem)",
              fontWeight: 800,
              lineHeight: 1,
              outline: "none",
            }}
            type="text"
            value={inputValue}
          />
          <span
            className="mt-1 text-[0.8125rem] font-semibold leading-5 text-text-secondary"
            id="weight-unit"
          >
            kg
          </span>
        </label>

        <div className="mt-2 min-h-5" aria-live="polite">
          {showError ? (
            <p className="text-sm leading-5 text-error" id="weight-error">
              Ingresá un peso válido mayor a 0.
            </p>
          ) : null}
        </div>
      </div>

      <div className="mt-2 flex flex-col items-center gap-2.5 md:mt-3">
        <Button
          className="px-4 text-text-secondary"
          onClick={handleSkip}
          size="sm"
          type="button"
          variant="ghost"
        >
          Omitir por ahora
        </Button>
        <Button
          disabled={validWeight === undefined}
          fullWidth
          onClick={handleContinue}
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
