import { apiRequest } from "@/services/api-client";
import type { CoachProgressResponse } from "../types/coach-progress.types";

export function getCoachProgress(
  exerciseId: string,
  authToken: string,
): Promise<CoachProgressResponse> {
  return apiRequest<CoachProgressResponse>(
    `/api/coach/progress/exercises/${encodeURIComponent(exerciseId)}`,
    {
      authToken,
      cache: "no-store",
      method: "GET",
    },
  );
}
