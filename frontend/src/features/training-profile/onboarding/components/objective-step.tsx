"use client";

import { ArrowLeft } from "lucide-react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import {
  ObjetivoEntrenamiento,
  type ObjetivoEntrenamiento as ObjetivoEntrenamientoValue,
} from "../types/onboarding.types";

const objectiveOptions: ReadonlyArray<{
  description: string;
  title: string;
  value: ObjetivoEntrenamientoValue;
}> = [
  {
    description: "Aumentar tamaño y desarrollo muscular",
    title: "Ganar masa muscular",
    value: ObjetivoEntrenamiento.GanarMasaMuscular,
  },
  {
    description: "Mejorar tu rendimiento y mover más peso",
    title: "Ganar fuerza",
    value: ObjetivoEntrenamiento.GanarFuerza,
  },
  {
    description: "Progresar en ambos objetivos",
    title: "Masa muscular y fuerza",
    value: ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
  },
  {
    description: "Mejorar tu estado físico general",
    title: "Acondicionamiento general",
    value: ObjetivoEntrenamiento.AcondicionamientoGeneral,
  },
];

export function ObjectiveStep() {
  const router = useRouter();
  const { draft, setObjetivo } = useOnboarding();

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <header className="flex items-center gap-3">
        <button
          aria-label="Volver"
          className="inline-flex size-12 shrink-0 items-center justify-center rounded-control border border-border bg-surface text-text-primary transition-colors hover:border-border-strong hover:bg-surface-elevated"
          onClick={() => router.push("/onboarding")}
          type="button"
        >
          <ArrowLeft aria-hidden="true" size={20} strokeWidth={2} />
        </button>

        <div className="flex min-w-0 flex-1 items-center gap-3">
          <div
            aria-label="Paso 1 de 10"
            aria-valuemax={10}
            aria-valuemin={0}
            aria-valuenow={1}
            className="h-1.5 flex-1 overflow-hidden rounded-full bg-surface-elevated"
            role="progressbar"
          >
            <div className="h-full w-[10%] rounded-full bg-primary" />
          </div>
          <span className="shrink-0 text-xs font-medium text-text-secondary">
            1 de 10
          </span>
        </div>
      </header>

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuál es tu objetivo principal?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí lo que más querés priorizar.
        </p>
      </div>

      <fieldset className="mt-6 space-y-3 md:mt-8">
        <legend className="sr-only">Objetivo principal</legend>
        {objectiveOptions.map((option) => {
          const isSelected = draft.objetivo === option.value;

          return (
            <label className="block cursor-pointer" key={option.value}>
              <input
                checked={isSelected}
                className="peer sr-only"
                name="objetivo"
                onChange={() => setObjetivo(option.value)}
                type="radio"
                value={option.value}
              />
              <span
                className={cn(
                  "flex min-h-[76px] items-center gap-4 rounded-card border bg-surface px-4 py-3.5 transition-[background-color,border-color,box-shadow] duration-200 hover:border-border-strong peer-focus-visible:ring-2 peer-focus-visible:ring-primary peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-primary bg-primary/5"
                    : "border-border",
                )}
              >
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-semibold text-text-primary sm:text-base">
                    {option.title}
                  </span>
                  <span className="mt-1 block text-xs leading-5 text-text-secondary sm:text-sm">
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
          disabled={draft.objetivo === undefined}
          fullWidth
          type="button"
          variant="gradient"
        >
          Continuar
        </Button>
      </div>
    </article>
  );
}
