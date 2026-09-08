"use client";

import type { CSSProperties } from "react";
import { EyeOff, Mars, Venus, type LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  Sexo,
  type Sexo as SexoValue,
} from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

const primaryOptions: ReadonlyArray<{
  accent: "feminine" | "masculine";
  icon: LucideIcon;
  title: string;
  value: SexoValue;
}> = [
  {
    accent: "masculine",
    icon: Mars,
    title: "Masculino",
    value: Sexo.Masculino,
  },
  {
    accent: "feminine",
    icon: Venus,
    title: "Femenino",
    value: Sexo.Femenino,
  },
];

export function SexStep() {
  const { draft, setSexo } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding/weight",
    nextPath: "/onboarding/summary",
  });
  const selectedSex = draft.sexo;

  const handleContinue = () => {
    if (selectedSex !== undefined) {
      setSexo(selectedSex);
      goNext();
    }
  };

  return (
    <article
      className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[640px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center"
      style={
        {
          "--sex-feminine":
            "color-mix(in srgb, var(--primary) 55%, var(--error))",
        } as CSSProperties
      }
    >
      <OnboardingStepHeader
        currentStep={10}
        onBack={goBack}
      />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Querés indicarnos tu sexo?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Esta información nos ayuda a personalizar mejor tu experiencia.
        </p>
      </div>

      <fieldset className="mt-6 md:mt-8">
        <legend className="sr-only">Sexo</legend>

        <div className="grid grid-cols-2 gap-4">
          {primaryOptions.map((option) => {
            const isSelected = selectedSex === option.value;
            const Icon = option.icon;

            return (
              <label className="block cursor-pointer" key={option.value}>
                <input
                  checked={isSelected}
                  className="peer sr-only"
                  name="sexo"
                  onChange={() => setSexo(option.value)}
                  type="radio"
                  value={option.value}
                />
                <span
                  className={cn(
                    "flex min-h-40 flex-col items-center justify-center gap-4 rounded-card border px-4 py-6 text-center transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                    !isSelected && "border-border bg-surface",
                    isSelected &&
                      option.accent === "masculine" &&
                      "border-secondary bg-[color-mix(in_srgb,var(--secondary)_9%,var(--surface))]",
                    isSelected &&
                      option.accent === "feminine" &&
                      "border-[var(--sex-feminine)] bg-[color-mix(in_srgb,var(--sex-feminine)_9%,var(--surface))]",
                  )}
                >
                  <span
                    className={cn(
                      "flex size-16 items-center justify-center rounded-control transition-[background-color,color] duration-200",
                      option.accent === "masculine" &&
                        "bg-[color-mix(in_srgb,var(--secondary)_9%,var(--surface-elevated))] text-[color-mix(in_srgb,var(--secondary)_72%,var(--text-secondary))]",
                      option.accent === "feminine" &&
                        "bg-[color-mix(in_srgb,var(--sex-feminine)_9%,var(--surface-elevated))] text-[color-mix(in_srgb,var(--sex-feminine)_72%,var(--text-secondary))]",
                      isSelected &&
                        option.accent === "masculine" &&
                        "bg-[color-mix(in_srgb,var(--secondary)_17%,var(--surface-elevated))] text-secondary",
                      isSelected &&
                        option.accent === "feminine" &&
                        "bg-[color-mix(in_srgb,var(--sex-feminine)_17%,var(--surface-elevated))] text-[var(--sex-feminine)]",
                    )}
                  >
                    <Icon
                      aria-hidden="true"
                      size={40}
                      strokeWidth={2}
                    />
                  </span>
                  <span className="text-base leading-5 font-semibold text-text-primary">
                    {option.title}
                  </span>
                </span>
              </label>
            );
          })}
        </div>

        <label className="mt-4 block cursor-pointer">
          <input
            checked={selectedSex === Sexo.PrefieroNoInformarlo}
            className="peer sr-only"
            name="sexo"
            onChange={() => setSexo(Sexo.PrefieroNoInformarlo)}
            type="radio"
            value={Sexo.PrefieroNoInformarlo}
          />
          <span
            className={cn(
              "flex min-h-18 items-center gap-3 rounded-card border px-4 py-3 transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
              selectedSex === Sexo.PrefieroNoInformarlo
                ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))]"
                : "border-border bg-surface",
            )}
          >
            <span
              className={cn(
                "flex size-10 shrink-0 items-center justify-center rounded-control-sm bg-surface-elevated transition-colors",
                selectedSex === Sexo.PrefieroNoInformarlo
                  ? "text-primary"
                  : "text-text-secondary",
              )}
            >
              <EyeOff aria-hidden="true" size={22} strokeWidth={2} />
            </span>
            <span className="text-base leading-5 font-semibold text-text-primary">
              Prefiero no informarlo
            </span>
          </span>
        </label>
      </fieldset>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={selectedSex === undefined}
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
