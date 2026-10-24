"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getExerciseProgressHub } from "../services/progress.service";
import type { ExerciseProgressHub } from "../types/progress.types";

export type GetExerciseProgressHubActionResult =
  | { status: "ready"; progress: ExerciseProgressHub }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getExerciseProgressHubAction(): Promise<GetExerciseProgressHubActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    return { status: "ready", progress: await getExerciseProgressHub(token) };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
