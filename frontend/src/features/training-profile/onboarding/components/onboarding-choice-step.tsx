"use client";

import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { OnboardingStepHeader } from "./onboarding-step-header";

export type OnboardingChoiceOption<T extends number> = Readonly<{
  description: string;
  icon?: ReactNode;
  title: string;
  value: T;
}>;

type OnboardingChoiceStepProps<T extends number> = {
  currentStep: number;
  legend: string;
  name: string;
  onBack: () => void;
  onChange: (value: T) => void;
  onContinue?: () => void;
  options: ReadonlyArray<OnboardingChoiceOption<T>>;
  question: string;
  selectedValue?: T;
  supportingText: string;
};

export function OnboardingChoiceStep<T extends number>({
  currentStep,
  legend,
  name,
  onBack,
  onChange,
  onContinue,
  options,
  question,
  selectedValue,
  supportingText,
}: OnboardingChoiceStepProps<T>) {
  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={currentStep} onBack={onBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          {question}
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          {supportingText}
        </p>
      </div>

      <fieldset className="mt-6 space-y-3 md:mt-8">
        <legend className="sr-only">{legend}</legend>
        {options.map((option) => {
          const isSelected = selectedValue === option.value;

          return (
            <label className="block cursor-pointer" key={option.value}>
              <input
                checked={isSelected}
                className="peer sr-only"
                name={name}
                onChange={() => onChange(option.value)}
                type="radio"
                value={option.value}
              />
              <span
                className={cn(
                  "flex min-h-[76px] items-center gap-4 rounded-card border bg-surface px-4 py-4 transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-primary/5"
                    : "border-border",
                )}
              >
                {option.icon ? (
                  <span
                    aria-hidden="true"
                    className={cn(
                      "flex size-9 shrink-0 items-center justify-center transition-colors",
                      isSelected ? "text-primary" : "text-text-secondary",
                    )}
                  >
                    {option.icon}
                  </span>
                ) : null}
                <span className="min-w-0 flex-1">
                  <span className="block text-base font-semibold text-text-primary">
                    {option.title}
                  </span>
                  <span className="mt-1.5 block text-sm leading-5 text-text-secondary">
                    {option.description}
                  </span>
                </span>
                <span
                  aria-hidden="true"
                  className={cn(
                    "flex size-5 shrink-0 items-center justify-center rounded-full border",
                    isSelected ? "border-primary" : "border-border-strong",
                  )}
                >
                  {isSelected ? (
                    <span className="size-2.5 rounded-full bg-primary" />
                  ) : null}
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={selectedValue === undefined}
          fullWidth
          onClick={onContinue}
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}
