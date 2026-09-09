"use client";

import { useState, type ChangeEvent } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import { OnboardingStepHeader } from "./onboarding-step-header";

const minimumAge = 1;
const maximumAge = 120;

export function AgeStep() {
  const { draft, setEdad } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/equipment",
    nextPath: "/onboarding/weight",
  });
  const [inputValue, setInputValue] = useState(
    draft.edad?.toString() ?? "",
  );
  const [hasBlurred, setHasBlurred] = useState(false);
  const validAge = parseAge(inputValue);
  const showError =
    hasBlurred && inputValue.length > 0 && validAge === undefined;

  const handleChange = (event: ChangeEvent<HTMLInputElement>) => {
    const nextValue = event.target.value;
    const nextAge = parseAge(nextValue);

    setInputValue(nextValue);
    setHasBlurred(false);
    setEdad(nextAge);
  };

  const handleBlur = () => {
    setHasBlurred(true);

    if (validAge !== undefined) {
      setInputValue(validAge.toString());
    }
  };

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader
        currentStep={8}
        onBack={goBack}
      />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuántos años tenés?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Esto nos ayuda a adaptar mejor tu entrenamiento.
        </p>
      </div>

      <div className="mt-10 flex flex-col items-center md:mt-12">
        <div
          className={cn(
            "flex h-28 w-44 items-center justify-center rounded-card border bg-surface px-4 transition-[background-color,border-color,box-shadow] duration-200 focus-within:border-primary focus-within:bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))] focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-2 focus-within:ring-offset-background",
            showError && "border-error",
          )}
        >
          <label className="sr-only" htmlFor="edad">
            Edad en años
          </label>
          <input
            aria-describedby={showError ? "age-unit age-error" : "age-unit"}
            aria-invalid={showError}
            autoComplete="off"
            className="w-full appearance-none bg-transparent text-center text-text-primary outline-none placeholder:text-text-secondary [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
            id="edad"
            inputMode="numeric"
            max={maximumAge}
            min={minimumAge}
            onBlur={handleBlur}
            onChange={handleChange}
            placeholder="—"
            step={1}
            style={{
              fontFamily: "var(--font-raleway), sans-serif",
              fontSize: "clamp(3rem, 4vw, 3.5rem)",
              fontWeight: 700,
              lineHeight: 1,
            }}
            type="number"
            value={inputValue}
          />
        </div>

        <p
          className="mt-2 text-base leading-6 font-semibold text-text-secondary"
          id="age-unit"
        >
          años
        </p>

        <div className="mt-2 min-h-5" aria-live="polite">
          {showError ? (
            <p className="text-sm leading-5 text-error" id="age-error">
              Ingresá una edad entre 1 y 120.
            </p>
          ) : null}
        </div>
      </div>

      <div className="mt-8">
        <Button
          className="min-h-14"
          disabled={validAge === undefined}
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

function parseAge(value: string): number | undefined {
  if (!/^\d+$/.test(value)) {
    return undefined;
  }

  const age = Number(value);

  if (!Number.isInteger(age) || age < minimumAge || age > maximumAge) {
    return undefined;
  }

  return age;
}
