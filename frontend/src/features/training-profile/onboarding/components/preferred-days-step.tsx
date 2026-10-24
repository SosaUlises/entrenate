"use client";

import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { dayOptions } from "../../shared/training-profile-options";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import type { DiaSemana as DiaSemanaValue } from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

export function PreferredDaysStep() {
  const { draft, setDiasPreferidos } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/training-days",
    nextPath: "/onboarding/duration",
  });
  const maximumDays = draft.diasEntrenamientoPorSemana;
  const selectedDays = draft.diasPreferidos;
  const selectedCount = selectedDays?.length ?? 0;
  const hasReachedMaximum =
    maximumDays !== undefined && selectedCount >= maximumDays;
  const hasNoFixedDays = selectedDays?.length === 0;
  const isAnswerValid =
    selectedDays !== undefined &&
    maximumDays !== undefined &&
    selectedCount <= maximumDays;

  const toggleDay = (day: DiaSemanaValue) => {
    const currentDays = selectedDays ?? [];

    if (currentDays.includes(day)) {
      const nextDays = currentDays.filter((selectedDay) => selectedDay !== day);
      setDiasPreferidos(nextDays.length > 0 ? nextDays : undefined);
      return;
    }

    if (maximumDays === undefined || currentDays.length >= maximumDays) {
      return;
    }

    setDiasPreferidos([...currentDays, day]);
  };

  const toggleNoFixedDays = () => {
    setDiasPreferidos(hasNoFixedDays ? undefined : []);
  };

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={4} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Qué días preferís entrenar?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí hasta {maximumDays ?? 0} {maximumDays === 1 ? "día" : "días"}.
        </p>
        <p className="mt-1 text-sm leading-6 text-text-secondary">
          Podés cambiarlos más adelante.
        </p>
      </div>

      <fieldset className="mt-6 grid grid-cols-8 gap-2 md:mt-8">
        <legend className="sr-only">Días preferidos</legend>
        {dayOptions.map((day, index) => {
          const isSelected = selectedDays?.includes(day.value) ?? false;
          const isDisabled = hasReachedMaximum && !isSelected;
          const centeredSecondRowClass =
            index === 4
              ? "col-start-2"
              : index === 5
                ? "col-start-4"
                : index === 6
                  ? "col-start-6"
                  : undefined;

          return (
            <label
              className={cn(
                "col-span-2 block",
                centeredSecondRowClass,
                isDisabled ? "cursor-not-allowed" : "cursor-pointer",
              )}
              key={day.value}
            >
              <input
                checked={isSelected}
                className="peer sr-only"
                disabled={isDisabled}
                name="diasPreferidos"
                onChange={() => toggleDay(day.value)}
                type="checkbox"
                value={day.value}
              />
              <span
                className={cn(
                  "relative flex h-[3.75rem] items-center justify-center rounded-control border px-2 text-base font-bold transition-[background-color,border-color,color,opacity,box-shadow,transform] duration-200 active:scale-[0.98] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-thermal-accent/40 bg-surface-active text-text-primary shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                    : "border-border bg-surface-interactive text-text-secondary",
                  isDisabled
                    ? "opacity-45"
                    : "hover:border-border-strong hover:text-text-primary",
                )}
              >
                {day.label}
                <span
                  aria-hidden="true"
                  className={cn(
                    "absolute right-1.5 top-1.5 flex size-4 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
                    isSelected
                      ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                      : "scale-90 border-border-strong/70 bg-surface text-transparent",
                  )}
                >
                  <Check
                    className={cn(
                      "transition-transform duration-200",
                      isSelected ? "scale-100" : "scale-0",
                    )}
                    size={9}
                    strokeWidth={2.5}
                  />
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      {!hasNoFixedDays ? (
        <p
          aria-live="polite"
          className="mt-3 text-[0.8125rem] font-medium leading-5 text-text-muted"
        >
          {selectedCount} de {maximumDays ?? 0} seleccionados
        </p>
      ) : null}

      <button
        aria-pressed={hasNoFixedDays}
        className={cn(
          "mt-7 flex min-h-14 w-full items-center border-y px-1 text-left text-sm font-semibold transition-[background-color,border-color,color] duration-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus",
          hasNoFixedDays
            ? "border-thermal-accent/35 bg-surface-interactive/70 text-text-primary"
            : "border-border bg-transparent text-text-secondary hover:bg-surface-interactive hover:text-text-primary",
        )}
        onClick={toggleNoFixedDays}
        type="button"
      >
        <span className="flex-1">No tengo días fijos</span>
        <span
          aria-hidden="true"
          className={cn(
            "flex size-5 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
            hasNoFixedDays
              ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
              : "scale-95 border-border-strong bg-surface text-transparent",
          )}
        >
          <Check
            className={cn(
              "transition-transform duration-200",
              hasNoFixedDays ? "scale-100" : "scale-0",
            )}
            size={12}
            strokeWidth={2.5}
          />
        </span>
      </button>

      <div className="mt-6 md:mt-7">
        <Button
          disabled={!isAnswerValid}
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
