"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getCoachProgress } from "../services/coach-progress.service";
import type { CoachProgressResponse } from "../types/coach-progress.types";

export type GetCoachProgressActionResult =
  | { status: "ready"; progress: CoachProgressResponse }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getCoachProgressAction(
  exerciseId: string,
): Promise<GetCoachProgressActionResult> {
  const token = await getAuthToken();

  if (!token) {
    return { status: "unauthenticated" };
  }

  try {
    return {
      status: "ready",
      progress: await getCoachProgress(exerciseId, token),
    };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }

      if (error.status === 404) {
        return { status: "not-found" };
      }
    }

    return { status: "error" };
  }
}
