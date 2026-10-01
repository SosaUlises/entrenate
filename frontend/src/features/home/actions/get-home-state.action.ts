"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { getRoutineSummaries } from "@/features/routines/services/routines.service";
import { getActiveTrainingSession } from "@/features/training/services/training.service";
import { getFirstMissingTarget } from "@/features/training/training-position";
import { ApiClientError } from "@/services/api-client";

export type HomeState =
  | {
      status: "active-session";
      readyToFinish: true;
      exerciseCount: number;
    }
  | {
      status: "active-session";
      readyToFinish: false;
      exerciseCount: number;
      exerciseName: string;
      exercisePosition: number;
      setNumber: number;
      targetSetCount: number;
    }
  | { status: "no-routines" }
  | { status: "routines-available"; routineCount: number }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getHomeStateAction(): Promise<HomeState> {
  const token = await getAuthToken();

  if (!token) {
    return { status: "unauthenticated" };
  }

  try {
    const session = await getActiveTrainingSession(token);
    const target = getFirstMissingTarget(session);

    if (!target) {
      return {
        status: "active-session",
        readyToFinish: true,
        exerciseCount: session.ejercicios.length,
      };
    }

    return {
      status: "active-session",
      readyToFinish: false,
      exerciseCount: session.ejercicios.length,
      exerciseName: target.exercise.nombre,
      exercisePosition: target.exercisePosition,
      setNumber: target.setNumber,
      targetSetCount: target.exercise.seriesObjetivo,
    };
  } catch (activeError) {
    if (activeError instanceof ApiClientError && activeError.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    if (!(activeError instanceof ApiClientError) || activeError.status !== 404) {
      return { status: "error" };
    }
  }

  try {
    const routines = await getRoutineSummaries(token);

    return routines.length === 0
      ? { status: "no-routines" }
      : { status: "routines-available", routineCount: routines.length };
  } catch (routinesError) {
    if (routinesError instanceof ApiClientError && routinesError.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
