"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { getMyTrainingProfile } from "@/features/training-profile/onboarding/services/training-profile.service";
import type { TrainingProfileResponse } from "@/features/training-profile/shared/training-profile.types";
import { ApiClientError } from "@/services/api-client";

export type GetRoutinePlannerProfileActionResult =
  | { profile: TrainingProfileResponse; status: "ready" }
  | { status: "error" | "missing" | "unauthenticated" };

export async function getRoutinePlannerProfileAction(): Promise<GetRoutinePlannerProfileActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    return {
      profile: await getMyTrainingProfile(authToken),
      status: "ready",
    };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }

      if (error.status === 404) return { status: "missing" };
    }

    return { status: "error" };
  }
}
