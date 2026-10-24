"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getExerciseProgressTrend } from "../services/progress.service";
import type { ExerciseProgressTrend } from "../types/progress.types";

export type GetExerciseProgressTrendActionResult =
  | { status: "ready"; trend: ExerciseProgressTrend }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getExerciseProgressTrendAction(
  exerciseId: string,
  range: { from?: string; to?: string; timeZone: string },
): Promise<GetExerciseProgressTrendActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    const trend = await getExerciseProgressTrend(exerciseId, range, token);
    return { status: "ready", trend };
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
