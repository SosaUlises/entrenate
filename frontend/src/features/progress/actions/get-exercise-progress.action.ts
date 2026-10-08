"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getExerciseProgress } from "../services/progress.service";
import type { ExerciseProgress } from "../types/progress.types";

export type GetExerciseProgressActionResult =
  | { status: "ready"; progress: ExerciseProgress }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getExerciseProgressAction(exerciseId: string): Promise<GetExerciseProgressActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    return { status: "ready", progress: await getExerciseProgress(exerciseId, token) };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) return { status: "not-found" };
    }

    return { status: "error" };
  }
}
