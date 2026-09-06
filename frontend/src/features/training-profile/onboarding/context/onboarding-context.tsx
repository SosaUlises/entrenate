"use client";

import { createContext, useContext, useState, type ReactNode } from "react";
import type {
  NivelExperiencia,
  ObjetivoEntrenamiento,
  OnboardingDraft,
} from "../types/onboarding.types";

type OnboardingContextValue = {
  draft: OnboardingDraft;
  setNivelExperiencia: (nivelExperiencia: NivelExperiencia) => void;
  setObjetivo: (objetivo: ObjetivoEntrenamiento) => void;
};

const OnboardingContext = createContext<OnboardingContextValue | null>(null);

export function OnboardingProvider({ children }: { children: ReactNode }) {
  const [draft, setDraft] = useState<OnboardingDraft>({});

  const setNivelExperiencia = (nivelExperiencia: NivelExperiencia) => {
    setDraft((currentDraft) => ({ ...currentDraft, nivelExperiencia }));
  };

  const setObjetivo = (objetivo: ObjetivoEntrenamiento) => {
    setDraft((currentDraft) => ({ ...currentDraft, objetivo }));
  };

  return (
    <OnboardingContext value={{ draft, setNivelExperiencia, setObjetivo }}>
      {children}
    </OnboardingContext>
  );
}

export function useOnboarding(): OnboardingContextValue {
  const context = useContext(OnboardingContext);

  if (!context) {
    throw new Error("useOnboarding debe utilizarse dentro de OnboardingProvider.");
  }

  return context;
}
