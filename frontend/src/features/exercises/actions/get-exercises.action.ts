"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getExercises } from "../services/exercises.service";
import type { Exercise } from "../types/exercise.types";

export type GetExercisesActionResult =
  | { status: "ready"; exercises: Exercise[] }
  | { status: "error" }
  | { status: "unauthenticated" };

export async function getExercisesAction(): Promise<GetExercisesActionResult> {
  const token = await getAuthToken();

  if (!token) {
    return { status: "unauthenticated" };
  }

  try {
    return { status: "ready", exercises: await getExercises(token) };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
