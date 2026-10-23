"use client";

import type { ReactNode } from "react";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/class-names";
import { useOnboarding } from "../context/onboarding-context";
import { useOnboardingStepNavigation } from "../hooks/use-onboarding-step-navigation";
import {
  ObjetivoEntrenamiento,
  type ObjetivoEntrenamiento as ObjetivoEntrenamientoValue,
} from "../types/onboarding.types";
import { OnboardingStepHeader } from "./onboarding-step-header";

// ─── Spot illustrations ───────────────────────────────────────────────────────
// All share viewBox="0 0 100 64". Rendered at h-14 w-auto inside a dedicated
// illustration zone (~5.5rem tall, ~40% of each card).
// Fill shapes at low opacity convey mass; thermalAccent marks one focal point.

function MuscleIllustration({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 100 64"
    >
      {/* Torso main body */}
      <path
        d="M36 56 L33 28 Q28 16 34 9 Q41 3 50 3 Q59 3 66 9 Q72 16 67 28 L64 56 Z"
        fill="currentColor"
        opacity="0.18"
      />
      {/* Left deltoid */}
      <ellipse
        cx="27"
        cy="22"
        fill="currentColor"
        opacity="0.16"
        rx="11"
        ry="8"
        transform="rotate(-18 27 22)"
      />
      {/* Right deltoid */}
      <ellipse
        cx="73"
        cy="22"
        fill="currentColor"
        opacity="0.16"
        rx="11"
        ry="8"
        transform="rotate(18 73 22)"
      />
      {/* Left bicep */}
      <ellipse
        cx="20"
        cy="37"
        fill="currentColor"
        opacity="0.13"
        rx="7.5"
        ry="13"
        transform="rotate(-8 20 37)"
      />
      {/* Right bicep */}
      <ellipse
        cx="80"
        cy="37"
        fill="currentColor"
        opacity="0.13"
        rx="7.5"
        ry="13"
        transform="rotate(8 80 37)"
      />
      {/* Left forearm taper */}
      <path
        d="M15 47 Q14 57 17 59 Q20 60 22 58 Q24 55 22 46 Z"
        fill="currentColor"
        opacity="0.1"
      />
      {/* Right forearm taper */}
      <path
        d="M85 47 Q86 57 83 59 Q80 60 78 58 Q76 55 78 46 Z"
        fill="currentColor"
        opacity="0.1"
      />
      {/* Trapezius curve */}
      <path
        d="M34 18 Q42 15 50 14 Q58 15 66 18"
        fill="none"
        opacity="0.30"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.6"
      />
      {/* Pec left arc */}
      <path
        d="M34 26 Q42 33 50 31"
        fill="none"
        opacity="0.42"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.5"
      />
      {/* Pec right arc */}
      <path
        d="M66 26 Q58 33 50 31"
        fill="none"
        opacity="0.42"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.5"
      />
      {/* Sternum line */}
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.2"
        x1="50"
        x2="50"
        y1="22"
        y2="33"
      />
      {/* Abs horizontal */}
      <line
        opacity="0.20"
        stroke="currentColor"
        strokeWidth="1"
        x1="40"
        x2="60"
        y1="43"
        y2="43"
      />
      {/* Abs vertical */}
      <line
        opacity="0.22"
        stroke="currentColor"
        strokeWidth="1"
        x1="46"
        x2="46"
        y1="36"
        y2="52"
      />
      <line
        opacity="0.22"
        stroke="currentColor"
        strokeWidth="1"
        x1="54"
        x2="54"
        y1="36"
        y2="52"
      />
      {/* ThermalAccent focal dot — sternum */}
      <circle cx="50" cy="31" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

function StrengthIllustration({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 100 64"
    >
      {/* Floor shadow */}
      <ellipse
        cx="50"
        cy="58"
        fill="currentColor"
        opacity="0.07"
        rx="42"
        ry="3"
      />
      {/* Main bar */}
      <rect
        fill="currentColor"
        height="7"
        opacity="0.24"
        rx="3.5"
        width="84"
        x="8"
        y="28.5"
      />
      {/* Left outer plate */}
      <rect
        fill="currentColor"
        height="32"
        opacity="0.21"
        rx="2.5"
        width="11"
        x="5"
        y="16"
      />
      {/* Left inner plate */}
      <rect
        fill="currentColor"
        height="26"
        opacity="0.17"
        rx="1.5"
        width="8"
        x="18"
        y="19"
      />
      {/* Right inner plate */}
      <rect
        fill="currentColor"
        height="26"
        opacity="0.17"
        rx="1.5"
        width="8"
        x="74"
        y="19"
      />
      {/* Right outer plate */}
      <rect
        fill="currentColor"
        height="32"
        opacity="0.21"
        rx="2.5"
        width="11"
        x="84"
        y="16"
      />
      {/* Left collar */}
      <rect
        fill="currentColor"
        height="13"
        opacity="0.22"
        rx="1"
        width="6"
        x="28"
        y="25.5"
      />
      {/* Right collar */}
      <rect
        fill="currentColor"
        height="13"
        opacity="0.22"
        rx="1"
        width="6"
        x="66"
        y="25.5"
      />
      {/* Left outer plate grooves */}
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="7"
        x2="14"
        y1="21"
        y2="21"
      />
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="7"
        x2="14"
        y1="25"
        y2="25"
      />
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="7"
        x2="14"
        y1="39"
        y2="39"
      />
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="7"
        x2="14"
        y1="43"
        y2="43"
      />
      {/* Right outer plate grooves */}
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="86"
        x2="93"
        y1="21"
        y2="21"
      />
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="86"
        x2="93"
        y1="25"
        y2="25"
      />
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="86"
        x2="93"
        y1="39"
        y2="39"
      />
      <line
        opacity="0.38"
        stroke="currentColor"
        strokeWidth="0.9"
        x1="86"
        x2="93"
        y1="43"
        y2="43"
      />
      {/* Knurl marks center grip */}
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeWidth="0.85"
        x1="38"
        x2="38"
        y1="28.5"
        y2="35.5"
      />
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeWidth="0.85"
        x1="42"
        x2="42"
        y1="28.5"
        y2="35.5"
      />
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeWidth="0.85"
        x1="46"
        x2="46"
        y1="28.5"
        y2="35.5"
      />
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeWidth="0.85"
        x1="54"
        x2="54"
        y1="28.5"
        y2="35.5"
      />
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeWidth="0.85"
        x1="58"
        x2="58"
        y1="28.5"
        y2="35.5"
      />
      <line
        opacity="0.28"
        stroke="currentColor"
        strokeWidth="0.85"
        x1="62"
        x2="62"
        y1="28.5"
        y2="35.5"
      />
      {/* ThermalAccent — center knurl ring */}
      <circle cx="50" cy="32" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

function CombinedIllustration({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 100 64"
    >
      {/* Floor shadow */}
      <ellipse
        cx="50"
        cy="61"
        fill="currentColor"
        opacity="0.07"
        rx="30"
        ry="2.5"
      />
      {/* Barbell bar (across shoulders / rack) */}
      <rect
        fill="currentColor"
        height="5.5"
        opacity="0.22"
        rx="2.75"
        width="74"
        x="13"
        y="19"
      />
      {/* Left plate large */}
      <rect
        fill="currentColor"
        height="18"
        opacity="0.19"
        rx="2"
        width="9"
        x="10"
        y="13"
      />
      {/* Left plate small */}
      <rect
        fill="currentColor"
        height="13"
        opacity="0.15"
        rx="1.5"
        width="6"
        x="21"
        y="15.5"
      />
      {/* Right plate small */}
      <rect
        fill="currentColor"
        height="13"
        opacity="0.15"
        rx="1.5"
        width="6"
        x="73"
        y="15.5"
      />
      {/* Right plate large */}
      <rect
        fill="currentColor"
        height="18"
        opacity="0.19"
        rx="2"
        width="9"
        x="81"
        y="13"
      />
      {/* Torso (broad shoulders, tapers to waist) */}
      <path
        d="M38 25 L36 55 L64 55 L62 25 Z"
        fill="currentColor"
        opacity="0.16"
      />
      {/* Shoulders width block */}
      <rect
        fill="currentColor"
        height="8"
        opacity="0.14"
        rx="4"
        width="38"
        x="31"
        y="19"
      />
      {/* Left arm gripping bar */}
      <path
        d="M35 22 Q28 23 25 28 Q23 33 26 37 Q29 39 33 37 L37 34"
        fill="currentColor"
        opacity="0.13"
      />
      {/* Right arm gripping bar */}
      <path
        d="M65 22 Q72 23 75 28 Q77 33 74 37 Q71 39 67 37 L63 34"
        fill="currentColor"
        opacity="0.13"
      />
      {/* Left leg */}
      <path
        d="M42 55 L39 62"
        opacity="0.15"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="6"
      />
      {/* Right leg */}
      <path
        d="M58 55 L61 62"
        opacity="0.15"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="6"
      />
      {/* Head */}
      <circle cx="50" cy="12" fill="currentColor" opacity="0.18" r="5.5" />
      {/* Sternum line */}
      <line
        opacity="0.26"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.2"
        x1="50"
        x2="50"
        y1="28"
        y2="42"
      />
      {/* Chest arcs */}
      <path
        d="M38 29 Q44 35 50 33"
        fill="none"
        opacity="0.38"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.4"
      />
      <path
        d="M62 29 Q56 35 50 33"
        fill="none"
        opacity="0.38"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.4"
      />
      {/* ThermalAccent — bar center grip */}
      <circle cx="50" cy="21.75" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

function ConditioningIllustration({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      fill="none"
      viewBox="0 0 100 64"
    >
      {/* Ground line */}
      <line
        opacity="0.18"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.2"
        x1="10"
        x2="90"
        y1="58"
        y2="58"
      />
      {/* Ground shadow */}
      <ellipse
        cx="52"
        cy="58"
        fill="currentColor"
        opacity="0.09"
        rx="20"
        ry="2.5"
      />
      {/* Head */}
      <circle cx="63" cy="8" fill="currentColor" opacity="0.20" r="5.5" />
      {/* Torso — leaning forward */}
      <path
        d="M52 14 L44 36 L50 38 L58 16 Z"
        fill="currentColor"
        opacity="0.17"
      />
      {/* Left arm — reaching forward */}
      <path
        d="M55 17 Q48 12 42 14 Q39 16 41 18"
        fill="none"
        opacity="0.15"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="5"
      />
      {/* Right arm — swept back */}
      <path
        d="M56 20 Q65 23 72 21 Q75 19 73 17"
        fill="none"
        opacity="0.15"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="5"
      />
      {/* Forward leg — stride */}
      <path
        d="M46 37 Q42 46 37 52 Q34 57 34 58"
        fill="none"
        opacity="0.16"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="5.5"
      />
      {/* Back leg — extended */}
      <path
        d="M49 36 Q52 44 58 50 Q62 55 60 58"
        fill="none"
        opacity="0.16"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="5.5"
      />
      {/* Forward knee joint */}
      <circle cx="39" cy="46" fill="currentColor" opacity="0.14" r="3" />
      {/* Speed lines */}
      <line
        opacity="0.22"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.6"
        x1="72"
        x2="84"
        y1="22"
        y2="22"
      />
      <line
        opacity="0.16"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1.4"
        x1="74"
        x2="85"
        y1="27"
        y2="27"
      />
      <line
        opacity="0.11"
        stroke="currentColor"
        strokeLinecap="round"
        strokeWidth="1"
        x1="70"
        x2="80"
        y1="32"
        y2="32"
      />
      {/* ThermalAccent — center of mass / core */}
      <circle cx="51" cy="26" fill="var(--thermal-accent)" r="2.5" />
    </svg>
  );
}

// ─── Data ─────────────────────────────────────────────────────────────────────

type ObjectiveOption = {
  description: string;
  illustration: (props: { className?: string }) => ReactNode;
  label: string;
  title: string;
  value: ObjetivoEntrenamientoValue;
};

const objectiveOptions: ReadonlyArray<ObjectiveOption> = [
  {
    description: "Desarrollo y volumen muscular",
    illustration: MuscleIllustration,
    label: "01",
    title: "Ganar masa muscular",
    value: ObjetivoEntrenamiento.GanarMasaMuscular,
  },
  {
    description: "Aumentar cargas y rendimiento",
    illustration: StrengthIllustration,
    label: "02",
    title: "Ganar fuerza",
    value: ObjetivoEntrenamiento.GanarFuerza,
  },
  {
    description: "Progresar en ambos objetivos",
    illustration: CombinedIllustration,
    label: "03",
    title: "Masa muscular y fuerza",
    value: ObjetivoEntrenamiento.GanarmasaMuscularYFuerza,
  },
  {
    description: "Mejorar tu condición física",
    illustration: ConditioningIllustration,
    label: "04",
    title: "Acondicionamiento general",
    value: ObjetivoEntrenamiento.AcondicionamientoGeneral,
  },
];

// ─── Component ────────────────────────────────────────────────────────────────

export function ObjectiveStep() {
  const { draft, setObjetivo } = useOnboarding();
  const { goBack, goNext } = useOnboardingStepNavigation({
    backPath: "/onboarding",
    nextPath: "/onboarding/experience",
  });
  const selectedObjective = draft.objetivo;

  return (
    <article className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[560px] flex-col sm:min-h-[calc(100dvh-4rem)] md:justify-center">
      <OnboardingStepHeader currentStep={1} onBack={goBack} />

      <div className="mt-7 sm:mt-8">
        <h1 className="font-brand text-[1.75rem] leading-tight font-bold tracking-tight text-text-primary sm:text-3xl md:text-[2rem]">
          ¿Cuál es tu objetivo principal?
        </h1>
        <p className="mt-2 text-sm leading-6 text-text-secondary sm:text-base md:mt-3">
          Elegí lo que más querés priorizar.
        </p>
      </div>

      <fieldset className="mx-auto mt-6 grid w-full max-w-[520px] grid-cols-2 gap-3 md:mt-8">
        <legend className="sr-only">Objetivo principal</legend>
        {objectiveOptions.map((option) => {
          const isSelected = selectedObjective === option.value;
          const Illustration = option.illustration;

          return (
            <label className="flex min-w-0 cursor-pointer" key={option.value}>
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
                  "flex w-full min-w-0 flex-col overflow-hidden rounded-card border transition-[border-color,box-shadow,transform] duration-200 active:scale-[0.99] peer-focus-visible:ring-2 peer-focus-visible:ring-thermal-accent peer-focus-visible:ring-offset-2 peer-focus-visible:ring-offset-background",
                  isSelected
                    ? "border-thermal-accent/40 bg-surface-active shadow-[inset_0_0_0_1px_rgba(225,115,78,0.18)]"
                    : "border-border bg-surface-interactive hover:border-border-strong",
                )}
              >
                {/* ── Illustration zone (~40% of card) ── */}
                <span
                  className={cn(
                    "relative flex h-[6.25rem] w-full shrink-0 items-center justify-center border-b transition-[border-color,background-color] duration-200",
                    isSelected
                      ? "border-thermal-accent/15 bg-surface-elevated/50"
                      : "border-border/60 bg-surface/50",
                  )}
                >
                  <Illustration
                    className={cn(
                      "h-[4rem] w-auto transition-colors duration-200",
                      isSelected ? "text-text-primary" : "text-text-secondary",
                    )}
                  />

                  {/* Check badge — affordance always visible, discreet */}
                  <span
                    aria-hidden="true"
                    className={cn(
                      "absolute top-2.5 right-2.5 flex size-5 items-center justify-center rounded-full border transition-[border-color,background-color,transform] duration-200",
                      isSelected
                        ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
                        : "scale-95 border-border/60 bg-surface/70 text-transparent",
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

                {/* ── Content zone ── */}
                <span className="flex flex-1 flex-col px-3.5 pb-4 pt-3.5">
                  {/* Index — very secondary */}
                  <span
                    className={cn(
                      "font-brand text-[0.65rem] font-bold tracking-[0.16em] transition-colors duration-200",
                      isSelected ? "text-thermal-accent/70" : "text-text-muted/55",
                    )}
                  >
                    {option.label}
                  </span>

                  {/* Title */}
                  <span
                    className={cn(
                      "font-brand mt-2 text-[0.9375rem] leading-snug tracking-tight transition-[color,font-weight] duration-200 sm:text-base",
                      isSelected
                        ? "font-extrabold text-text-primary"
                        : "font-bold text-text-primary/85",
                    )}
                  >
                    {option.title}
                  </span>

                  {/* Description */}
                  <span className="mt-1.5 text-[0.8125rem] leading-[1.45] text-text-secondary">
                    {option.description}
                  </span>
                </span>
              </span>
            </label>
          );
        })}
      </fieldset>

      <div className="mx-auto mt-7 w-full max-w-[520px]">
        <Button
          disabled={selectedObjective === undefined}
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
