"use server";

import { getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { createTrainingProfile } from "../services/training-profile.service";
import type { CreateTrainingProfileRequest } from "../types/training-profile.types";

export type CreateTrainingProfileActionResult =
  | { id: string; ok: true }
  | { kind: "already_exists"; ok: false }
  | { kind: "error"; message?: string; ok: false };

export async function createTrainingProfileAction(
  request: CreateTrainingProfileRequest,
): Promise<CreateTrainingProfileActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) {
    return { kind: "error", ok: false };
  }

  try {
    const response = await createTrainingProfile(authToken, request);

    return { id: response.id, ok: true };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 409) {
      return { kind: "already_exists", ok: false };
    }

    if (error instanceof ApiClientError && error.status === 400) {
      return {
        kind: "error",
        message: getFirstValidationMessage(error.details),
        ok: false,
      };
    }

    return { kind: "error", ok: false };
  }
}

function getFirstValidationMessage(details: unknown): string | undefined {
  if (!details || typeof details !== "object") {
    return undefined;
  }

  for (const value of Object.values(details)) {
    if (Array.isArray(value) && typeof value[0] === "string") {
      return value[0];
    }
  }

  return undefined;
}
