"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { synthesizeLiveSpeech } from "../services/live-coach.service";

export type SynthesizeLiveSpeechActionResult =
  | { audioBase64: string; contentType: string; status: "success" }
  | {
      message?: string;
      status: "error" | "network-error" | "not-found" | "unauthenticated" | "unavailable";
    };

export async function synthesizeLiveSpeechAction(
  responseId: string,
): Promise<SynthesizeLiveSpeechActionResult> {
  const authToken = await getAuthToken();
  if (!authToken) return { status: "unauthenticated" };

  try {
    const { arrayBuffer, contentType } = await synthesizeLiveSpeech(responseId, authToken);
    const audioBase64 = Buffer.from(arrayBuffer).toString("base64");
    return { audioBase64, contentType, status: "success" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) {
        return { message: "La respuesta no fue encontrada o expiró.", status: "not-found" };
      }
      if (error.status === 502 || error.status === 503 || error.status === 504) {
        return { message: "El servicio de voz no está disponible.", status: "unavailable" };
      }
      if (error.code === "network_error") {
        return { message: "Error de red al conectar con el servidor.", status: "network-error" };
      }
    }

    return {
      message: error instanceof Error ? error.message : "Error al sintetizar voz.",
      status: "error",
    };
  }
}

