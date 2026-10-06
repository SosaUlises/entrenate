"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getTrainingActivity } from "../services/history.service";
import type { TrainingActivity, TrainingActivityRequest } from "../types/history.types";

export type GetTrainingActivityActionResult =
  | { status: "ready"; activity: TrainingActivity }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getTrainingActivityAction(
  request: TrainingActivityRequest,
): Promise<GetTrainingActivityActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    return { status: "ready", activity: await getTrainingActivity(request, token) };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
