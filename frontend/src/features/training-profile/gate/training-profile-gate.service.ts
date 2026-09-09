import { ApiClientError } from "@/services/api-client";
import { getMyTrainingProfile } from "../onboarding/services/training-profile.service";
import {
  getDestinationForTrainingProfile,
  type TrainingProfilePresence,
} from "./training-profile-navigation";

export type TrainingProfileGateResolution =
  | {
      presence: TrainingProfilePresence;
      status: "resolved";
    }
  | {
      status: "error";
    };

export type PostAuthDestinationResolution =
  | {
      destination: string | null;
      presence: TrainingProfilePresence;
      status: "resolved";
    }
  | {
      status: "error";
    };

export async function resolveTrainingProfileGate(
  authToken: string,
): Promise<TrainingProfileGateResolution> {
  try {
    await getMyTrainingProfile(authToken);

    return { presence: "exists", status: "resolved" };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 404) {
      return { presence: "missing", status: "resolved" };
    }

    return { status: "error" };
  }
}

export async function resolvePostAuthDestination(
  authToken: string,
): Promise<PostAuthDestinationResolution> {
  const gateResolution = await resolveTrainingProfileGate(authToken);

  if (gateResolution.status === "error") {
    return gateResolution;
  }

  return {
    destination: getDestinationForTrainingProfile(gateResolution.presence),
    presence: gateResolution.presence,
    status: "resolved",
  };
}
