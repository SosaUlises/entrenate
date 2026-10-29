import { apiRequest } from "@/services/api-client";
import type {
  LiveCoachRespondRequest,
  LiveCoachRespondResponse,
  LiveCoachTranscriptionResponse,
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

