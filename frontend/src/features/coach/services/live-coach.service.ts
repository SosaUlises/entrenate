import { apiRequest } from "@/services/api-client";
import type { LiveCoachTranscriptionResponse } from "../types/live-coach.types";

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

