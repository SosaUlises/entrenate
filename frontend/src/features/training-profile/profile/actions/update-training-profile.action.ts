"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { updateTrainingProfile } from "../../onboarding/services/training-profile.service";
import type { UpdateTrainingProfileRequest } from "../../shared/training-profile.types";

export type UpdateTrainingProfileActionResult =
  | { ok: true }
  | {
      kind: "error" | "missing" | "unauthenticated" | "validation";
      message: string;
      ok: false;
    };

export async function updateTrainingProfileAction(
  request: UpdateTrainingProfileRequest,
): Promise<UpdateTrainingProfileActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) {
    return { kind: "unauthenticated", message: "Tu sesión venció.", ok: false };
  }

  try {
    await updateTrainingProfile(authToken, request);
    return { ok: true };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { kind: "unauthenticated", message: "Tu sesión venció.", ok: false };
      }

      if (error.status === 404) {
        return {
          kind: "missing",
          message: "Tu perfil de entrenamiento ya no está disponible.",
          ok: false,
        };
      }

      if (error.status === 400) {
        return {
          kind: "validation",
          message: getFirstValidationMessage(error.details) ?? "Revisá los datos ingresados.",
          ok: false,
        };
      }

      if (error.code === "network_error") {
        return {
          kind: "error",
          message: "No pudimos conectar con la API. Intentá nuevamente.",
          ok: false,
        };
      }
    }

    return {
      kind: "error",
      message: "No pudimos guardar los cambios. Intentá nuevamente.",
      ok: false,
    };
  }
}

function getFirstValidationMessage(details: unknown): string | undefined {
  if (!details || typeof details !== "object") return undefined;

  for (const messages of Object.values(details)) {
    if (Array.isArray(messages) && typeof messages[0] === "string") return messages[0];
  }

  return undefined;
}
