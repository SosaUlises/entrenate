"use client";

import { ArrowLeft } from "lucide-react";

type OnboardingStepHeaderProps = {
  currentStep: number;
  label?: string;
  onBack: () => void;
};

const totalSteps = 10;

export function OnboardingStepHeader({
  currentStep,
  label,
  onBack,
}: OnboardingStepHeaderProps) {
  const progressPercentage = (currentStep / totalSteps) * 100;

  return (
    <header className="flex items-center gap-3">
      <button
        aria-label="Volver"
        className="inline-flex size-12 shrink-0 items-center justify-center rounded-control border border-border bg-surface text-text-primary transition-colors hover:border-border-strong hover:bg-surface-elevated"
        onClick={onBack}
        type="button"
      >
        <ArrowLeft aria-hidden="true" size={20} strokeWidth={2} />
      </button>

      <div className="flex min-w-0 flex-1 items-center gap-3">
        <div
          aria-label={`Paso ${currentStep} de ${totalSteps}`}
          aria-valuemax={totalSteps}
          aria-valuemin={0}
          aria-valuenow={currentStep}
          className="h-1.5 flex-1 overflow-hidden rounded-full bg-surface-elevated"
          role="progressbar"
        >
          <div
            className="h-full rounded-full bg-primary"
            style={{ width: `${progressPercentage}%` }}
          />
        </div>
        <span className="shrink-0 text-xs font-medium text-text-secondary">
          {label ?? `${currentStep} de ${totalSteps}`}
        </span>
      </div>
    </header>
  );
}
