"use client";

import { Check, EyeOff, Mars, Venus, type LucideIcon } from "lucide-react";
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
  icon: LucideIcon;
  title: string;
  value: SexoValue;
}> = [
  {
    icon: Mars,
    title: "Masculino",
    value: Sexo.Masculino,
  },
  {
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
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={10} onBack={goBack} />

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

        <div className="grid grid-cols-2 gap-3.5">
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
                    "relative flex h-36 flex-col overflow-hidden rounded-card border text-center transition-[background-color,border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                    isSelected
                      ? "border-thermal-accent/40 bg-surface-active shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                      : "border-border bg-surface-interactive hover:border-border-strong",
                  )}
                >
                  <span
                    className={cn(
                      "flex h-[5.5rem] w-full shrink-0 items-center justify-center border-b transition-[background-color,border-color,color] duration-200",
                      isSelected
                        ? "border-thermal-accent/15 bg-surface-elevated/50 text-text-primary"
                        : "border-border/60 bg-surface/50 text-text-secondary",
                    )}
                  >
                    <Icon
                      aria-hidden="true"
                      size={48}
                      strokeWidth={1.8}
                    />
                  </span>
                  <span className="flex flex-1 items-center justify-center px-3 py-2 font-brand text-base font-bold leading-5 text-text-primary">
                    {option.title}
                  </span>

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
              "flex min-h-16 items-center gap-3 border-y px-1 py-3 transition-[background-color,border-color] duration-200 hover:bg-surface-interactive peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
              selectedSex === Sexo.PrefieroNoInformarlo
                ? "border-thermal-accent/35 bg-surface-active/60"
                : "border-border/70 bg-transparent",
            )}
          >
            <span
              className={cn(
                "flex size-10 shrink-0 items-center justify-center rounded-control-sm bg-surface-elevated transition-colors",
                selectedSex === Sexo.PrefieroNoInformarlo
                  ? "text-thermal-accent"
                  : "text-text-muted",
              )}
            >
              <EyeOff aria-hidden="true" size={22} strokeWidth={2} />
            </span>
            <span className="min-w-0 flex-1 text-sm font-semibold leading-5 text-text-primary sm:text-base">
              Prefiero no informarlo
            </span>
            <span
              aria-hidden="true"
              className={cn(
                "flex size-6 shrink-0 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-200",
                selectedSex === Sexo.PrefieroNoInformarlo
                  ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                  : "scale-95 border-border-strong bg-surface text-transparent",
              )}
            >
              <Check
                className={cn(
                  "transition-transform duration-200",
                  selectedSex === Sexo.PrefieroNoInformarlo
                    ? "scale-100"
                    : "scale-0",
                )}
                size={13}
                strokeWidth={2.25}
              />
            </span>
          </span>
        </label>
      </fieldset>

      <div className="mt-6 md:mt-8">
        <Button
          disabled={selectedSex === undefined}
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
