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
      <OnboardingStepHeader currentStep={8} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuántos años tenés?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Esto nos ayuda a adaptar mejor tu entrenamiento.
        </p>
      </div>

      <div className="mt-6 flex flex-col items-center md:mt-8">
        <label
          className={cn(
            "flex h-28 w-full max-w-[20rem] cursor-text flex-col items-center justify-center rounded-card border border-border bg-surface-interactive px-5 py-3 shadow-[inset_0_1px_0_rgba(255,255,255,0.04)] transition-[background-color,border-color,box-shadow] duration-200 focus-within:border-thermal-accent/45 focus-within:bg-surface-active focus-within:ring-2 focus-within:ring-thermal-accent focus-within:ring-offset-2 focus-within:ring-offset-background",
            showError && "border-error/70",
          )}
          htmlFor="edad"
        >
          <span className="sr-only">
            Edad en años
          </span>
          <input
            aria-describedby={showError ? "age-unit age-error" : "age-unit"}
            aria-invalid={showError}
            autoComplete="off"
            className="w-full max-w-52 appearance-none border-0 bg-transparent text-center font-brand font-extrabold tracking-[-0.04em] text-text-primary caret-thermal-accent shadow-none outline-none placeholder:text-text-muted focus:border-0 focus:outline-none focus:ring-0 focus-visible:outline-none focus-visible:ring-0 focus-visible:shadow-none [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
            id="edad"
            inputMode="numeric"
            max={maximumAge}
            min={minimumAge}
            onBlur={handleBlur}
            onChange={handleChange}
            placeholder="—"
            step={1}
            style={{
              border: 0,
              boxShadow: "none",
              fontFamily: "var(--font-raleway), sans-serif",
              fontSize: "clamp(3rem, 9vw, 3.5rem)",
              fontWeight: 800,
              lineHeight: 1,
              outline: "none",
            }}
            type="number"
            value={inputValue}
          />
          <span
            className="mt-1 text-[0.8125rem] font-semibold leading-5 text-text-secondary"
            id="age-unit"
          >
            años
          </span>
        </label>

        <div className="mt-2 min-h-5" aria-live="polite">
          {showError ? (
            <p className="text-sm leading-5 text-error" id="age-error">
              Ingresá una edad entre 1 y 120.
            </p>
          ) : null}
        </div>
      </div>

      <div className="mt-4 md:mt-5">
        <Button
          disabled={validAge === undefined}
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
