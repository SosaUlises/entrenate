"use server";

import { getAuthToken } from "@/features/auth/services/session.service";
import { resolveTrainingProfileGate } from "./training-profile-gate.service";
import type { TrainingProfilePresence } from "./training-profile-navigation";

export type CheckTrainingProfileActionResult =
  | {
      presence: TrainingProfilePresence;
      status: "resolved";
    }
  | {
      status: "error" | "unauthenticated";
    };

export async function checkTrainingProfileAction(): Promise<CheckTrainingProfileActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) {
    return { status: "unauthenticated" };
  }

  return resolveTrainingProfileGate(authToken);
}
