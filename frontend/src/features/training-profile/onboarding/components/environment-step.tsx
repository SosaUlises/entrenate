"use client";

import type { ReactNode } from "react";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  EntornoEntrenamiento,
  type EntornoEntrenamiento as EntornoEntrenamientoValue,
} from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

type IllustrationProps = {
  className?: string;
};

function HomeGymIllustration({ className }: IllustrationProps) {
  return (
    <svg aria-hidden="true" className={className} fill="none" viewBox="0 0 96 64">
      <ellipse cx="48" cy="57" fill="currentColor" opacity="0.08" rx="38" ry="3" />
      <path d="M14 31L48 8L82 31V55H14V31Z" fill="currentColor" opacity="0.1" />
      <path d="M24 31L48 15L72 31" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="3" opacity="0.24" />
      <rect x="29" y="42" width="38" height="6" rx="3" fill="currentColor" opacity="0.27" />
      <rect x="34" y="48" width="4" height="8" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="58" y="48" width="4" height="8" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="20" y="34" width="20" height="4" rx="2" fill="currentColor" opacity="0.22" />
      <path d="M18 30V42M42 30V42" stroke="currentColor" strokeLinecap="round" strokeWidth="3" opacity="0.18" />
      <circle cx="48" cy="45" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

function CompactGymIllustration({ className }: IllustrationProps) {
  return (
    <svg aria-hidden="true" className={className} fill="none" viewBox="0 0 96 64">
      <ellipse cx="48" cy="57" fill="currentColor" opacity="0.08" rx="39" ry="3" />
      <rect x="18" y="9" width="6" height="47" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="72" y="9" width="6" height="47" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="20" y="16" width="56" height="5" rx="2.5" fill="currentColor" opacity="0.28" />
      <rect x="12" y="25" width="72" height="5" rx="2.5" fill="currentColor" opacity="0.25" />
      <rect x="9" y="20" width="8" height="15" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="79" y="20" width="8" height="15" rx="2" fill="currentColor" opacity="0.2" />
      <path d="M32 55V43H64V55M28 43H68" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="4" opacity="0.18" />
      <circle cx="48" cy="27.5" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

function CalisthenicsIllustration({ className }: IllustrationProps) {
  return (
    <svg aria-hidden="true" className={className} fill="none" viewBox="0 0 96 64">
      <ellipse cx="48" cy="58" fill="currentColor" opacity="0.08" rx="40" ry="3" />
      <rect x="18" y="8" width="6" height="49" rx="3" fill="currentColor" opacity="0.2" />
      <rect x="72" y="8" width="6" height="49" rx="3" fill="currentColor" opacity="0.2" />
      <rect x="21" y="10" width="54" height="5" rx="2.5" fill="currentColor" opacity="0.3" />
      <path d="M32 57V34M64 57V34M30 35H66" stroke="currentColor" strokeLinecap="round" strokeWidth="4" opacity="0.2" />
      <path d="M38 35V48M58 35V48" stroke="currentColor" strokeLinecap="round" strokeWidth="3" opacity="0.15" />
      <rect x="10" y="54" width="22" height="4" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="64" y="54" width="22" height="4" rx="2" fill="currentColor" opacity="0.2" />
      <circle cx="48" cy="35" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

function CommercialGymIllustration({ className }: IllustrationProps) {
  return (
    <svg aria-hidden="true" className={className} fill="none" viewBox="0 0 96 64">
      <ellipse cx="48" cy="58" fill="currentColor" opacity="0.08" rx="42" ry="3" />
      <rect x="10" y="11" width="5" height="45" rx="2" fill="currentColor" opacity="0.18" />
      <rect x="40" y="11" width="5" height="45" rx="2" fill="currentColor" opacity="0.18" />
      <rect x="12" y="16" width="31" height="5" rx="2.5" fill="currentColor" opacity="0.27" />
      <rect x="6" y="25" width="43" height="5" rx="2.5" fill="currentColor" opacity="0.23" />
      <rect x="4" y="20" width="7" height="15" rx="2" fill="currentColor" opacity="0.2" />
      <rect x="44" y="20" width="7" height="15" rx="2" fill="currentColor" opacity="0.2" />
      <path d="M57 55V20H80V55M57 31H80" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="4" opacity="0.2" />
      <rect x="62" y="37" width="13" height="13" rx="3" fill="currentColor" opacity="0.22" />
      <path d="M23 55V45H44V55M20 45H47" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="3.5" opacity="0.17" />
      <circle cx="27.5" cy="27.5" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

const environmentOptions: ReadonlyArray<{
  description: string;
  illustration: (props: IllustrationProps) => ReactNode;
  title: string;
  value: EntornoEntrenamientoValue;
}> = [
  {
    description: "Entreno principalmente en casa",
    illustration: HomeGymIllustration,
    title: "Casa",
    value: EntornoEntrenamiento.Casa,
  },
  {
    description: "Equipamiento básico",
    illustration: CompactGymIllustration,
    title: "Gimnasio pequeño",
    value: EntornoEntrenamiento.GimnasioPequeno,
  },
  {
    description: "Entrenamiento con peso corporal",
    illustration: CalisthenicsIllustration,
    title: "Calistenia",
    value: EntornoEntrenamiento.Calistenia,
  },
  {
    description: "Gran variedad de equipos",
    illustration: CommercialGymIllustration,
    title: "Gimnasio comercial",
    value: EntornoEntrenamiento.GimnasioComercial,
  },
];

export function EnvironmentStep() {
  const { draft, setEntornoEntrenamiento } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/duration",
    nextPath: "/onboarding/equipment",
  });
  const selectedEnvironment = draft.entornoEntrenamiento;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={6} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Dónde entrenás normalmente?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí el lugar donde entrenás la mayor parte del tiempo.
        </p>
      </div>

      <fieldset className="mx-auto mt-6 grid w-full max-w-[520px] auto-rows-[12.5rem] grid-cols-2 gap-3 md:mt-8">
        <legend className="sr-only">Entorno principal de entrenamiento</legend>
        {environmentOptions.map((option) => {
          const isSelected = selectedEnvironment === option.value;
          const Illustration = option.illustration;

          return (
            <label
              className="block h-full min-w-0 cursor-pointer"
              key={option.value}
            >
              <input
                checked={isSelected}
                className="peer sr-only"
                name="entornoEntrenamiento"
                onChange={() => setEntornoEntrenamiento(option.value)}
                type="radio"
                value={option.value}
              />
              <span
                className={cn(
                  "relative flex h-full w-full min-w-0 flex-col overflow-hidden rounded-card border transition-[background-color,border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-thermal-accent/40 bg-surface-active shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                    : "border-border bg-surface-interactive hover:border-border-strong",
                )}
              >
                <span
                  className={cn(
                    "relative flex h-[5.75rem] w-full shrink-0 items-center justify-center border-b transition-[background-color,border-color] duration-200",
                    isSelected
                      ? "border-thermal-accent/15 bg-surface-elevated/50"
                      : "border-border/60 bg-surface/50",
                  )}
                >
                  <Illustration
                    className={cn(
                      "h-[4.5rem] w-[6.75rem] transition-colors duration-200",
                      isSelected ? "text-text-primary" : "text-text-secondary",
                    )}
                  />

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

                <span className="flex flex-1 flex-col px-3.5 pb-3.5 pt-3 text-left">
                  <span
                    className={cn(
                      "font-brand text-[0.9375rem] font-bold leading-5 tracking-tight text-text-primary sm:text-base",
                      isSelected && "font-extrabold",
                    )}
                  >
                    {option.title}
                  </span>
                  <span className="mt-1 text-[0.8125rem] leading-[1.4] text-text-secondary">
                    {option.description}
                  </span>
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <p className="mx-auto mt-4 w-full max-w-[520px] text-[0.8125rem] font-medium leading-5 text-text-muted">
        Después elegimos qué equipamiento tenés disponible.
      </p>

      <div className="mx-auto mt-6 w-full max-w-[520px]">
        <Button
          disabled={selectedEnvironment === undefined}
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
