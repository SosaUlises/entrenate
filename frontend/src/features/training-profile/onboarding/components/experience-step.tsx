"use client";

import type { ReactNode } from "react";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  NivelExperiencia,
  type NivelExperiencia as NivelExperienciaValue,
} from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

type IllustrationProps = {
  className?: string;
};

function FoundationIllustration({ className }: IllustrationProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 96 64"
    >
      <ellipse cx="48" cy="57" fill="currentColor" opacity="0.07" rx="35" ry="3" />
      <path d="M18 20H78L70 52H26L18 20Z" fill="currentColor" opacity="0.14" />
      <path d="M23 25H73M26 34H70M29 43H67" stroke="currentColor" strokeWidth="1" opacity="0.2" />
      <path d="M38 29C42 27 45 29 45 33V44C45 47 42 49 39 47L35 44C33 42 34 38 36 36L38 29Z" fill="currentColor" opacity="0.25" />
      <path d="M58 29C54 27 51 29 51 33V44C51 47 54 49 57 47L61 44C63 42 62 38 60 36L58 29Z" fill="currentColor" opacity="0.25" />
      <line x1="48" x2="48" y1="18" y2="51" stroke="currentColor" strokeDasharray="2 3" opacity="0.25" />
      <circle cx="48" cy="22" r="2.75" fill="var(--thermal-accent)" />
    </svg>
  );
}

function BeginnerIllustration({ className }: IllustrationProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 96 64"
    >
      <ellipse cx="48" cy="55" fill="currentColor" opacity="0.08" rx="31" ry="3" />
      <g transform="rotate(-12 48 32)">
        <rect x="31" y="28.5" width="34" height="7" rx="3.5" fill="currentColor" opacity="0.25" />
        <path d="M18 22L29 25V39L18 42L12 36V28L18 22Z" fill="currentColor" opacity="0.2" />
        <path d="M78 22L67 25V39L78 42L84 36V28L78 22Z" fill="currentColor" opacity="0.2" />
        <rect x="27" y="25" width="7" height="14" rx="2" fill="currentColor" opacity="0.16" />
        <rect x="62" y="25" width="7" height="14" rx="2" fill="currentColor" opacity="0.16" />
        <circle cx="48" cy="32" r="2.75" fill="var(--thermal-accent)" />
      </g>
    </svg>
  );
}

function IntermediateIllustration({ className }: IllustrationProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 96 64"
    >
      <ellipse cx="48" cy="56" fill="currentColor" opacity="0.08" rx="39" ry="3" />
      <rect x="8" y="29" width="80" height="6" rx="3" fill="currentColor" opacity="0.25" />
      <rect x="10" y="15" width="10" height="34" rx="2.5" fill="currentColor" opacity="0.22" />
      <rect x="21" y="20" width="8" height="24" rx="2" fill="currentColor" opacity="0.17" />
      <rect x="67" y="20" width="8" height="24" rx="2" fill="currentColor" opacity="0.17" />
      <rect x="76" y="15" width="10" height="34" rx="2.5" fill="currentColor" opacity="0.22" />
      <path d="M38 29V35M43 29V35M53 29V35M58 29V35" stroke="currentColor" strokeWidth="1" opacity="0.34" />
      <circle cx="48" cy="32" r="3" fill="var(--thermal-accent)" />
    </svg>
  );
}

function AdvancedIllustration({ className }: IllustrationProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 96 64"
    >
      <ellipse cx="48" cy="58" fill="currentColor" opacity="0.08" rx="41" ry="3" />
      <rect x="16" y="8" width="6" height="48" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="74" y="8" width="6" height="48" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="13" y="53" width="15" height="4" rx="2" fill="currentColor" opacity="0.22" />
      <rect x="68" y="53" width="15" height="4" rx="2" fill="currentColor" opacity="0.22" />
      <path d="M22 18H29V23H22M74 18H67V23H74" fill="currentColor" opacity="0.3" />
      <rect x="7" y="18" width="82" height="5" rx="2.5" fill="currentColor" opacity="0.3" />
      <rect x="5" y="9" width="9" height="23" rx="2.5" fill="currentColor" opacity="0.25" />
      <rect x="82" y="9" width="9" height="23" rx="2.5" fill="currentColor" opacity="0.25" />
      <rect x="15" y="13" width="6" height="15" rx="2" fill="currentColor" opacity="0.16" />
      <rect x="75" y="13" width="6" height="15" rx="2" fill="currentColor" opacity="0.16" />
      <path d="M38 18V23M43 18V23M53 18V23M58 18V23" stroke="currentColor" strokeWidth="1" opacity="0.4" />
      <circle cx="48" cy="20.5" r="2.75" fill="var(--thermal-accent)" />
    </svg>
  );
}

type ExperienceOption = {
  description: string;
  illustration: (props: IllustrationProps) => ReactNode;
  title: string;
  value: NivelExperienciaValue;
};

const experienceOptions: ReadonlyArray<ExperienceOption> = [
  {
    description: "Empezás desde cero.",
    illustration: FoundationIllustration,
    title: "Sin experiencia",
    value: NivelExperiencia.SinExperiencia,
  },
  {
    description: "Ya entrenaste y seguís aprendiendo.",
    illustration: BeginnerIllustration,
    title: "Principiante",
    value: NivelExperiencia.Principiante,
  },
  {
    description: "Entrenás con regularidad y dominás lo básico.",
    illustration: IntermediateIllustration,
    title: "Intermedio",
    value: NivelExperiencia.Intermedio,
  },
  {
    description: "Tenés experiencia sólida en técnica y progresión.",
    illustration: AdvancedIllustration,
    title: "Avanzado",
    value: NivelExperiencia.Avanzado,
  },
];

export function ExperienceStep() {
  const { draft, setNivelExperiencia } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/objective",
    nextPath: "/onboarding/training-days",
  });
  const selectedExperience = draft.nivelExperiencia;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={2} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] font-bold leading-tight tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuánta experiencia tenés entrenando?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí la opción que mejor te represente.
        </p>
      </div>

      <fieldset className="mt-5 space-y-2.5 sm:mt-6">
        <legend className="sr-only">Nivel de experiencia</legend>
        {experienceOptions.map((option) => {
          const isSelected = selectedExperience === option.value;
          const Illustration = option.illustration;

          return (
            <label className="block cursor-pointer" key={option.value}>
              <input
                checked={isSelected}
                className="peer sr-only"
                name="nivelExperiencia"
                onChange={() => setNivelExperiencia(option.value)}
                type="radio"
                value={option.value}
              />
              <span
                className={cn(
                  "grid h-[5.25rem] grid-cols-[4.5rem_minmax(0,1fr)_1.5rem] items-center gap-3 rounded-card border px-3.5 transition-[background-color,border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-thermal-accent/40 bg-surface-active shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                    : "border-border bg-surface-interactive hover:border-border-strong",
                )}
              >
                <span
                  aria-hidden="true"
                  className={cn(
                    "flex h-14 w-[4.5rem] items-center justify-center border-r pr-3 transition-[border-color,color] duration-200",
                    isSelected
                      ? "border-thermal-accent/20 text-text-primary"
                      : "border-border/70 text-text-secondary",
                  )}
                >
                  <Illustration className="h-12 w-16" />
                </span>

                <span className="min-w-0">
                  <span
                    className={cn(
                      "block font-brand text-base leading-5 tracking-tight transition-[color,font-weight] duration-200",
                      isSelected
                        ? "font-extrabold text-text-primary"
                        : "font-bold text-text-primary/90",
                    )}
                  >
                    {option.title}
                  </span>
                  <span className="mt-1 block text-[0.8125rem] leading-[1.35] text-text-secondary">
                    {option.description}
                  </span>
                </span>

                <span
                  aria-hidden="true"
                  className={cn(
                    "flex size-6 shrink-0 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
                    isSelected
                      ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                      : "scale-95 border-border-strong bg-surface text-transparent",
                  )}
                >
                  <Check
                    className={cn(
                      "transition-transform duration-200",
                      isSelected ? "scale-100" : "scale-0",
                    )}
                    size={13}
                    strokeWidth={2.25}
                  />
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <div className="mt-5 sm:mt-6">
        <Button
          disabled={selectedExperience === undefined}
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
