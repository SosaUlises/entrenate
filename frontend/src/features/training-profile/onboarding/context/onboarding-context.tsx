"use client";

import {
  createContext,
  useCallback,
  useContext,
  useState,
  type ReactNode,
} from "react";
import type {
  DiaSemana,
  DiasEntrenamientoPorSemana,
  DuracionSesionMinutos,
  EntornoEntrenamiento,
  NivelExperiencia,
  ObjetivoEntrenamiento,
  OnboardingDraft,
  Sexo,
} from "../types/onboarding.types";

type OnboardingContextValue = {
  draft: OnboardingDraft;
  setDiasEntrenamientoPorSemana: (
    diasEntrenamientoPorSemana: DiasEntrenamientoPorSemana,
  ) => void;
  setDiasPreferidos: (diasPreferidos: DiaSemana[] | undefined) => void;
  setDuracionSesionMinutos: (
    duracionSesionMinutos: DuracionSesionMinutos,
  ) => void;
  setEntornoEntrenamiento: (
    entornoEntrenamiento: EntornoEntrenamiento,
  ) => void;
  setEdad: (edad: number | undefined) => void;
  setEquipamientoIds: (equipamientoIds: string[] | undefined) => void;
  setNivelExperiencia: (nivelExperiencia: NivelExperiencia) => void;
  setObjetivo: (objetivo: ObjetivoEntrenamiento) => void;
  setPesoKg: (pesoKg: number | null | undefined) => void;
  setSexo: (sexo: Sexo) => void;
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

  const setDuracionSesionMinutos = (
    duracionSesionMinutos: DuracionSesionMinutos,
  ) => {
    setDraft((currentDraft) => ({ ...currentDraft, duracionSesionMinutos }));
  };

  const setEntornoEntrenamiento = (
    entornoEntrenamiento: EntornoEntrenamiento,
  ) => {
    setDraft((currentDraft) => ({ ...currentDraft, entornoEntrenamiento }));
  };

  const setEdad = (edad: number | undefined) => {
    setDraft((currentDraft) => ({ ...currentDraft, edad }));
  };

  const setEquipamientoIds = useCallback(
    (equipamientoIds: string[] | undefined) => {
      setDraft((currentDraft) => ({ ...currentDraft, equipamientoIds }));
    },
    [],
  );

  const setNivelExperiencia = (nivelExperiencia: NivelExperiencia) => {
    setDraft((currentDraft) => ({ ...currentDraft, nivelExperiencia }));
  };

  const setObjetivo = (objetivo: ObjetivoEntrenamiento) => {
    setDraft((currentDraft) => ({ ...currentDraft, objetivo }));
  };

  const setPesoKg = (pesoKg: number | null | undefined) => {
    setDraft((currentDraft) => ({ ...currentDraft, pesoKg }));
  };

  const setSexo = (sexo: Sexo) => {
    setDraft((currentDraft) => ({ ...currentDraft, sexo }));
  };

  return (
    <OnboardingContext
      value={{
        draft,
        setDiasEntrenamientoPorSemana,
        setDiasPreferidos,
        setDuracionSesionMinutos,
        setEntornoEntrenamiento,
        setEdad,
        setEquipamientoIds,
        setNivelExperiencia,
        setObjetivo,
        setPesoKg,
        setSexo,
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
