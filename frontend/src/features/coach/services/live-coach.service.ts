import { ApiClientError, apiRequest } from "@/services/api-client";
import { env } from "@/lib/env";
import type {
  LiveCoachRespondRequest,
  LiveCoachRespondResponse,
  LiveCoachActionConfirmation,
  LiveCoachTranscriptionResponse,
  LiveCoachProactiveEvaluation,
  LiveCoachProactiveRequest,
  PostSessionCoachAnswer,
  PostSessionCoachSummary,
} from "../types/live-coach.types";

export function transcribeLiveAudio(
  formData: FormData,
  authToken: string,
): Promise<LiveCoachTranscriptionResponse> {
  return apiRequest<LiveCoachTranscriptionResponse, FormData>(
    "/api/coach/live/transcriptions",
    {
      authToken,
      body: formData,
      cache: "no-store",
      method: "POST",
    },
  );
}

export function evaluateLiveCoachProactive(
  payload: LiveCoachProactiveRequest,
  authToken: string,
): Promise<LiveCoachProactiveEvaluation> {
  return apiRequest<LiveCoachProactiveEvaluation, LiveCoachProactiveRequest>(
    "/api/coach/live/proactive",
    {
      authToken,
      body: payload,
      cache: "no-store",
      method: "POST",
    },
  );
}

export function respondLiveCoach(
  payload: LiveCoachRespondRequest,
  authToken: string,
): Promise<LiveCoachRespondResponse> {
  return apiRequest<LiveCoachRespondResponse, LiveCoachRespondRequest>(
    "/api/coach/live/respond",
    {
      authToken,
      body: payload,
      cache: "no-store",
      method: "POST",
    },
  );
}

export function confirmLiveCoachAction(
  actionId: string,
  restTimerIsActive: boolean,
  authToken: string,
): Promise<LiveCoachActionConfirmation> {
  return apiRequest<LiveCoachActionConfirmation, { restTimerIsActive: boolean }>(
    `/api/coach/live/actions/${encodeURIComponent(actionId)}/confirm`,
    {
      authToken,
      body: { restTimerIsActive },
      cache: "no-store",
      method: "POST",
    },
  );
}

export function cancelLiveCoachAction(
  actionId: string,
  authToken: string,
): Promise<void> {
  return apiRequest<void>(
    `/api/coach/live/actions/${encodeURIComponent(actionId)}/cancel`,
    { authToken, cache: "no-store", method: "POST" },
  );
}

export function getPostSessionCoachSummary(
  sessionId: string,
  authToken: string,
): Promise<PostSessionCoachSummary> {
  return apiRequest<PostSessionCoachSummary>(
    `/api/coach/live/sessions/${encodeURIComponent(sessionId)}/summary`,
    { authToken, cache: "no-store", method: "GET" },
  );
}

export function answerPostSessionCoachQuestion(
  sessionId: string,
  question: string,
  authToken: string,
): Promise<PostSessionCoachAnswer> {
  return apiRequest<PostSessionCoachAnswer, { question: string }>(
    `/api/coach/live/sessions/${encodeURIComponent(sessionId)}/summary/respond`,
    { authToken, body: { question }, cache: "no-store", method: "POST" },
  );
}

export async function synthesizeLiveSpeech(
  responseId: string,
  authToken: string,
): Promise<{ arrayBuffer: ArrayBuffer; contentType: string }> {
  if (!env.apiUrl) {
    throw new ApiClientError("La URL de la API no está configurada.", {
      code: "missing_api_url",
    });
  }

  const baseUrl = env.apiUrl.replace(/\/+$/, "");
  let response: Response;
  try {
    response = await fetch(`${baseUrl}/api/coach/live/speech`, {
      body: JSON.stringify({ responseId }),
      cache: "no-store",
      headers: {
        Accept: "audio/mpeg",
        Authorization: `Bearer ${authToken}`,
        "Content-Type": "application/json",
      },
      method: "POST",
    });
  } catch {
    throw new ApiClientError("No pudimos conectar con la API de voz.", {
      code: "network_error",
    });
  }

  if (!response.ok) {
    throw new ApiClientError(
      response.status === 404
        ? "La respuesta solicitada no existe o ha expirado."
        : "El servicio de voz no está disponible en este momento.",
      {
        code: `http_${response.status}`,
        status: response.status,
      },
    );
  }

  const contentType = response.headers.get("Content-Type") || "audio/mpeg";
  const arrayBuffer = await response.arrayBuffer();
  return { arrayBuffer, contentType };
}

