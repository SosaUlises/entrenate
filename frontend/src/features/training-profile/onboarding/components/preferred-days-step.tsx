"use client";

import { CalendarOff, Check } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { DiaSemana, type DiaSemana as DiaSemanaValue } from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

const dayOptions: ReadonlyArray<{
  label: string;
  value: DiaSemanaValue;
}> = [
  { label: "Lun", value: DiaSemana.Lunes },
  { label: "Mar", value: DiaSemana.Martes },
  { label: "Mié", value: DiaSemana.Miercoles },
  { label: "Jue", value: DiaSemana.Jueves },
  { label: "Vie", value: DiaSemana.Viernes },
  { label: "Sáb", value: DiaSemana.Sabado },
  { label: "Dom", value: DiaSemana.Domingo },
];

export function PreferredDaysStep() {
  const router = useRouter();
  const { draft, setDiasPreferidos } = useOnboarding();
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
      <OnboardingStepHeader
        currentStep={4}
        onBack={() => router.push("/onboarding/training-days")}
      />

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
                  "flex min-h-14 items-center justify-center gap-1 rounded-control border px-2 text-base font-semibold transition-[background-color,border-color,color,opacity,box-shadow] duration-200 peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))] text-text-primary"
                    : "border-border bg-surface text-text-secondary",
                  isDisabled
                    ? "opacity-45"
                    : "hover:border-border-strong hover:text-text-primary",
                )}
              >
                {isSelected ? (
                  <Check aria-hidden="true" size={14} strokeWidth={2.5} />
                ) : null}
                {day.label}
              </span>
            </label>
          );
        })}
      </fieldset>

      {!hasNoFixedDays ? (
        <p
          aria-live="polite"
          className="mt-3 text-sm leading-5 text-text-secondary"
        >
          {selectedCount} de {maximumDays ?? 0} seleccionados
        </p>
      ) : null}

      <button
        aria-pressed={hasNoFixedDays}
        className={cn(
          "mt-5 flex min-h-14 w-full items-center gap-3 rounded-card border px-4 text-left text-sm font-semibold transition-[background-color,border-color,color,box-shadow] duration-200 focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background",
          hasNoFixedDays
            ? "border-primary bg-[color-mix(in_srgb,var(--primary)_7%,var(--surface))] text-text-primary"
            : "border-border bg-surface text-text-secondary hover:border-border-strong hover:text-text-primary",
        )}
        onClick={toggleNoFixedDays}
        type="button"
      >
        <CalendarOff aria-hidden="true" size={20} strokeWidth={2} />
        <span className="flex-1">No tengo días fijos</span>
        <span
          aria-hidden="true"
          className={cn(
            "flex size-5 items-center justify-center rounded-full border",
            hasNoFixedDays ? "border-primary text-primary" : "border-border-strong",
          )}
        >
          {hasNoFixedDays ? <Check size={13} strokeWidth={2.5} /> : null}
        </span>
      </button>

      <div className="mt-6 md:mt-8">
        <Button
          className="min-h-14"
          disabled={!isAnswerValid}
          fullWidth
          onClick={() => router.push("/onboarding/duration")}
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}
