"use client";

import { createContext, useContext, useState, type ReactNode } from "react";
import type {
  DiaSemana,
  DiasEntrenamientoPorSemana,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  OnboardingDraft,
} from "../types/onboarding.types";

type OnboardingContextValue = {
  draft: OnboardingDraft;
  setDiasEntrenamientoPorSemana: (
    diasEntrenamientoPorSemana: DiasEntrenamientoPorSemana,
  ) => void;
  setDiasPreferidos: (diasPreferidos: DiaSemana[] | undefined) => void;
  setNivelExperiencia: (nivelExperiencia: NivelExperiencia) => void;
  setObjetivo: (objetivo: ObjetivoEntrenamiento) => void;
};

const OnboardingContext = createContext<OnboardingContextValue | null>(null);

export function OnboardingProvider({ children }: { children: ReactNode }) {
  const [draft, setDraft] = useState<OnboardingDraft>({});

  const setDiasEntrenamientoPorSemana = (
    diasEntrenamientoPorSemana: DiasEntrenamientoPorSemana,
  ) => {
    setDraft((currentDraft) => ({
      ...currentDraft,
      diasEntrenamientoPorSemana,
      diasPreferidos:
        currentDraft.diasPreferidos !== undefined &&
        currentDraft.diasPreferidos.length > diasEntrenamientoPorSemana
          ? undefined
          : currentDraft.diasPreferidos,
    }));
  };

  const setDiasPreferidos = (diasPreferidos: DiaSemana[] | undefined) => {
    setDraft((currentDraft) => ({ ...currentDraft, diasPreferidos }));
  };

  const setNivelExperiencia = (nivelExperiencia: NivelExperiencia) => {
    setDraft((currentDraft) => ({ ...currentDraft, nivelExperiencia }));
  };

  const setObjetivo = (objetivo: ObjetivoEntrenamiento) => {
    setDraft((currentDraft) => ({ ...currentDraft, objetivo }));
  };

  return (
    <OnboardingContext
      value={{
        draft,
        setDiasEntrenamientoPorSemana,
        setDiasPreferidos,
        setNivelExperiencia,
        setObjetivo,
      }}
    >
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
