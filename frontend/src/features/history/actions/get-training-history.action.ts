"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getTrainingHistory } from "../services/history.service";
import type { TrainingSessionHistorySummary } from "../types/history.types";

export type GetTrainingHistoryActionResult =
  | { status: "ready"; sessions: TrainingSessionHistorySummary[] }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getTrainingHistoryAction(): Promise<GetTrainingHistoryActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    return { status: "ready", sessions: await getTrainingHistory(token) };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
