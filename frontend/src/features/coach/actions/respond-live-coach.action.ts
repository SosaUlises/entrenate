"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { respondLiveCoach } from "../services/live-coach.service";
import type { LiveCoachRespondRequest, LiveCoachRespondResponse } from "../types/live-coach.types";

export type RespondLiveCoachActionResult =
  | { data: LiveCoachRespondResponse; status: "success" }
  | {
      message?: string;
      status:
        | "conflict"
        | "error"
        | "network-error"
        | "not-found"
        | "timeout"
        | "unauthenticated"
        | "unavailable";
    };

export async function respondLiveCoachAction(
  payload: LiveCoachRespondRequest,
): Promise<RespondLiveCoachActionResult> {
  const authToken = await getAuthToken();
  if (!authToken) return { status: "unauthenticated" };

  try {
    const data = await respondLiveCoach(payload, authToken);
    return { data, status: "success" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) {
        return { message: "Sesión de entrenamiento no encontrada.", status: "not-found" };
      }
      if (error.status === 409) {
        return { message: error.message || "La sesión de entrenamiento no está activa.", status: "conflict" };
      }
      if (error.status === 504) {
        return { message: "El servicio de Coach agotó el tiempo de espera.", status: "timeout" };
      }
      if (error.status === 502 || error.status === 503) {
        return { message: "El servicio de Coach no está disponible en este momento.", status: "unavailable" };
      }
      if (error.code === "network_error") {
        return { message: "Error de red al conectar con el servidor.", status: "network-error" };
      }
    }

    return {
      message: error instanceof Error ? error.message : "Ocurrió un error al procesar la respuesta del Coach.",
      status: "error",
    };
  }
}

