"use client";

import {
  createContext,
  useCallback,
  useContext,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from "react";
import { diasEntrenamientoPorSemanaValues } from "../types/onboarding.types";
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

const trainingDaysStorageKey = "entrenate:onboarding:training-days";
const trainingDaysListeners = new Set<() => void>();
let cachedTrainingDaysRaw: string | null | undefined;
let cachedTrainingDays: DiasEntrenamientoPorSemana | undefined;

function subscribeToStoredTrainingDays(listener: () => void) {
  trainingDaysListeners.add(listener);
  return () => {
    trainingDaysListeners.delete(listener);
  };
}

function getStoredTrainingDays(): DiasEntrenamientoPorSemana | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }

  try {
    const rawValue = window.sessionStorage.getItem(trainingDaysStorageKey);

    if (rawValue === cachedTrainingDaysRaw) {
      return cachedTrainingDays;
    }

    cachedTrainingDaysRaw = rawValue;
    const parsedValue = Number(rawValue);
    cachedTrainingDays = diasEntrenamientoPorSemanaValues.includes(
      parsedValue as DiasEntrenamientoPorSemana,
    )
      ? (parsedValue as DiasEntrenamientoPorSemana)
      : undefined;

    return cachedTrainingDays;
  } catch {
    return undefined;
  }
}

function persistTrainingDays(value: DiasEntrenamientoPorSemana) {
  cachedTrainingDaysRaw = String(value);
  cachedTrainingDays = value;

  try {
    window.sessionStorage.setItem(trainingDaysStorageKey, String(value));
  } catch {
    // The in-memory onboarding flow remains available if storage is blocked.
  }

  trainingDaysListeners.forEach((listener) => listener());
}

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
  const storedTrainingDays = useSyncExternalStore(
    subscribeToStoredTrainingDays,
    getStoredTrainingDays,
    () => undefined,
  );
  const resolvedDraft =
    draft.diasEntrenamientoPorSemana === undefined &&
    storedTrainingDays !== undefined
      ? {
          ...draft,
          diasEntrenamientoPorSemana: storedTrainingDays,
        }
      : draft;

  const setDiasEntrenamientoPorSemana = (
    diasEntrenamientoPorSemana: DiasEntrenamientoPorSemana,
  ) => {
    persistTrainingDays(diasEntrenamientoPorSemana);
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
        draft: resolvedDraft,
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
