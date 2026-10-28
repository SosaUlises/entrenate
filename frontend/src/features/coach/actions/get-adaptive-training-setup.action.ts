"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { getRoutineById } from "@/features/routines/services/routines.service";
import type { RoutineDetail } from "@/features/routines/types/routine.types";
import { getEquipment } from "@/features/training-profile/onboarding/services/equipment.service";
import { getMyTrainingProfile } from "@/features/training-profile/onboarding/services/training-profile.service";
import type {
  Equipment,
  TrainingProfileResponse,
} from "@/features/training-profile/shared/training-profile.types";
import { ApiClientError } from "@/services/api-client";

export type AdaptiveTrainingSetupData = {
  day: RoutineDetail["dias"][number];
  equipment: Equipment[];
  profile: TrainingProfileResponse;
  routine: Pick<RoutineDetail, "activa" | "id" | "nombre">;
};

export type GetAdaptiveTrainingSetupActionResult =
  | { data: AdaptiveTrainingSetupData; status: "ready" }
  | { status: "error" }
  | { status: "missing-profile" }
  | { status: "not-found" }
  | { status: "unauthenticated" };

export async function getAdaptiveTrainingSetupAction(
  routineId: string,
  routineDayId: string,
): Promise<GetAdaptiveTrainingSetupActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  const [routineResult, profileResult, equipmentResult] = await Promise.allSettled([
    getRoutineById(routineId, authToken),
    getMyTrainingProfile(authToken),
    getEquipment(authToken),
  ]);
  const errors = [routineResult, profileResult, equipmentResult]
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
    return { status: "missing-profile" };
  }

  if (
    routineResult.status === "rejected" &&
    routineResult.reason instanceof ApiClientError &&
    routineResult.reason.status === 404
  ) {
    return { status: "not-found" };
  }

  if (
    routineResult.status === "rejected" ||
    profileResult.status === "rejected" ||
    equipmentResult.status === "rejected"
  ) {
    return { status: "error" };
  }

  const routine = routineResult.value;
  const profile = profileResult.value;
  const equipment = equipmentResult.value;
  const day = routine.dias.find((item) => item.id === routineDayId);

  if (!day) return { status: "not-found" };

  return {
    data: {
      day,
      equipment,
      profile,
      routine: {
        activa: routine.activa,
        id: routine.id,
        nombre: routine.nombre,
      },
    },
    status: "ready",
  };
}

