"use server";

import { getCurrentUser } from "@/features/auth/services/auth.service";
import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import type { CurrentUser } from "@/features/auth/types/auth.types";
import { ApiClientError } from "@/services/api-client";
import { getEquipment } from "../../onboarding/services/equipment.service";
import { getMyTrainingProfile } from "../../onboarding/services/training-profile.service";
import type {
  Equipment,
  TrainingProfileResponse,
} from "../../shared/training-profile.types";

export type GetProfilePageActionResult =
  | {
      equipment: Equipment[] | null;
      profile: TrainingProfileResponse;
      status: "ready";
      user: CurrentUser;
    }
  | { status: "error" | "missing" | "unauthenticated" };

export async function getProfilePageAction(): Promise<GetProfilePageActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  const [userResult, profileResult, equipmentResult] = await Promise.allSettled([
    getCurrentUser(authToken),
    getMyTrainingProfile(authToken),
    getEquipment(authToken),
  ]);

  const errors = [userResult, profileResult, equipmentResult]
    .filter((result): result is PromiseRejectedResult => result.status === "rejected")
    .map((result) => result.reason);

  if (errors.some((error) => error instanceof ApiClientError && error.status === 401)) {
    await clearAuthSession();
    return { status: "unauthenticated" };
  }

  if (
    profileResult.status === "rejected" &&
    profileResult.reason instanceof ApiClientError &&
    profileResult.reason.status === 404
  ) {
    return { status: "missing" };
  }

  if (userResult.status === "rejected" || profileResult.status === "rejected") {
    return { status: "error" };
  }

  return {
    equipment: equipmentResult.status === "fulfilled" ? equipmentResult.value : null,
    profile: profileResult.value,
    status: "ready",
    user: userResult.value,
  };
}
