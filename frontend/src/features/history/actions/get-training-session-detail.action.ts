"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import type { TrainingSession } from "@/features/training/types/training.types";
import { ApiClientError } from "@/services/api-client";
import { getTrainingSessionById } from "../services/history.service";

export type GetTrainingSessionDetailActionResult =
  | { status: "ready"; session: TrainingSession }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getTrainingSessionDetailAction(id: string): Promise<GetTrainingSessionDetailActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    return { status: "ready", session: await getTrainingSessionById(id, token) };
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
