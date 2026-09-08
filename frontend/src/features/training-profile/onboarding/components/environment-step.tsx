"use client";

import { forwardRef } from "react";
import {
  Check,
  Dumbbell,
  House,
  PersonStanding,
  type LucideIcon,
  type LucideProps,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  EntornoEntrenamiento,
  type EntornoEntrenamiento as EntornoEntrenamientoValue,
} from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

const GymRackIcon = forwardRef<SVGSVGElement, LucideProps>(
  ({ color = "currentColor", size = 24, strokeWidth = 2, ...props }, ref) => (
    <svg
      fill="none"
      height={size}
      ref={ref}
      stroke={color}
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth={strokeWidth}
      viewBox="0 0 24 24"
      width={size}
      xmlns="http://www.w3.org/2000/svg"
      {...props}
    >
      <path d="M5 21V4h14v17" />
      <path d="M3 21h5M16 21h5" />
      <path d="M2 9h20" />
      <path d="M3 7v4M5 6.5v5M19 6.5v5M21 7v4" />
      <path d="M5 14h3M16 14h3" />
    </svg>
  ),
);

GymRackIcon.displayName = "GymRackIcon";

const environmentOptions: ReadonlyArray<{
  description: string;
  icon: LucideIcon;
  title: string;
  value: EntornoEntrenamientoValue;
}> = [
  {
    description: "Entreno principalmente en casa",
    icon: House,
    title: "Casa",
    value: EntornoEntrenamiento.Casa,
  },
  {
    description: "Equipamiento básico",
    icon: Dumbbell,
    title: "Gimnasio pequeño",
    value: EntornoEntrenamiento.GimnasioPequeno,
  },
  {
    description: "Entrenamiento con peso corporal",
    icon: PersonStanding,
    title: "Calistenia",
    value: EntornoEntrenamiento.Calistenia,
  },
  {
    description: "Gran variedad de equipos",
    icon: GymRackIcon,
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
      <OnboardingStepHeader
        currentStep={6}
        onBack={goBack}
      />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Dónde entrenás normalmente?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí el lugar donde entrenás la mayor parte del tiempo.
        </p>
      </div>

      <fieldset className="mt-6 space-y-3 md:mt-8">
        <legend className="sr-only">Entorno principal de entrenamiento</legend>
        {environmentOptions.map((option) => {
          const isSelected = selectedEnvironment === option.value;
          const Icon = option.icon;

          return (
            <label className="block cursor-pointer" key={option.value}>
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
                  "relative flex min-h-20 items-center gap-4 rounded-card border px-4 py-3 text-left transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))]"
                    : "border-border bg-surface",
                )}
              >
                {isSelected ? (
                  <Check
                    aria-hidden="true"
                    className="absolute top-1/2 right-4 -translate-y-1/2 text-primary"
                    size={16}
                    strokeWidth={2.5}
                  />
                ) : null}

                <span
                  aria-hidden="true"
                  className={cn(
                    "flex size-[46px] shrink-0 items-center justify-center rounded-control-sm border border-border transition-colors",
                    isSelected
                      ? "bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface-elevated))] text-primary"
                      : "bg-surface-elevated text-text-secondary",
                  )}
                >
                  <Icon size={27} strokeWidth={2} />
                </span>

                <span className="min-w-0 flex-1 pr-6">
                  <span className="block text-base leading-5 font-semibold text-text-primary">
                    {option.title}
                  </span>
                  <span className="mt-1 block text-[13px] leading-5 text-text-secondary sm:text-sm">
                    {option.description}
                  </span>
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <p className="mt-5 text-sm leading-6 text-text-secondary">
        Después elegimos qué equipamiento tenés disponible.
      </p>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={selectedEnvironment === undefined}
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
