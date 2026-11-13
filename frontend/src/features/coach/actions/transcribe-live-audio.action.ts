"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { transcribeLiveAudio } from "../services/live-coach.service";
import type { LiveCoachTranscriptionResponse } from "../types/live-coach.types";

export type TranscribeLiveAudioActionResult =
  | { data: LiveCoachTranscriptionResponse; status: "success" }
  | {
      message?: string;
      status:
        | "empty"
        | "error"
        | "network-error"
        | "timeout"
        | "too-large"
        | "too-long"
        | "unauthenticated"
        | "unavailable"
        | "unsupported";
    };

export async function transcribeLiveAudioAction(
  formData: FormData,
): Promise<TranscribeLiveAudioActionResult> {
  const authToken = await getAuthToken();
  if (!authToken) return { status: "unauthenticated" };

  try {
    const data = await transcribeLiveAudio(formData, authToken);
    return { data, status: "success" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 422) {
        return { message: "No se detectó habla en la grabación.", status: "empty" };
      }
      if (error.status === 413) {
        return { message: "El audio supera el límite de 2 MB.", status: "too-large" };
      }
      if (error.status === 400) {
        return { message: error.message, status: "unsupported" };
      }
      if (error.status === 504) {
        return { message: "El servicio de transcripción agotó el tiempo de espera.", status: "timeout" };
      }
      if (error.status === 502 || error.status === 503) {
        return { message: "El servicio de audio no está disponible en este momento.", status: "unavailable" };
      }
      if (error.code === "network_error") {
        return { message: "Error de red al conectar con el servidor.", status: "network-error" };
      }
    }

    return { message: "Ocurrió un error al procesar el audio.", status: "error" };
  }
}

