"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import type { TrainingSession } from "@/features/training/types/training.types";
import { ApiClientError } from "@/services/api-client";
import { cancelLiveCoachAction, confirmLiveCoachAction } from "../services/live-coach.service";

export type ConfirmLiveCoachPendingActionResult =
  | { data: TrainingSession; status: "success" }
  | { status: "conflict" | "error" | "not-found" | "unauthenticated" };

export async function confirmLiveCoachPendingAction(
  actionId: string,
): Promise<ConfirmLiveCoachPendingActionResult> {
  const authToken = await getAuthToken();
  if (!authToken) return { status: "unauthenticated" };

  try {
    return { data: await confirmLiveCoachAction(actionId, authToken), status: "success" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) return { status: "not-found" };
      if (error.status === 409) return { status: "conflict" };
    }
    return { status: "error" };
  }
}

export async function cancelLiveCoachPendingActionRequest(actionId: string): Promise<void> {
  const authToken = await getAuthToken();
  if (!authToken) return;

  try {
    await cancelLiveCoachAction(actionId, authToken);
  } catch {
    // Cancellation remains safe locally; the server-side action expires after its short TTL.
  }
}
