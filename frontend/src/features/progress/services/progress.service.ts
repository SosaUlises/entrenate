import { apiRequest } from "@/services/api-client";
import type { ExerciseProgress, ExerciseProgressHub, ExerciseProgressTrend } from "../types/progress.types";

export function getExerciseProgressHub(authToken: string): Promise<ExerciseProgressHub> {
  return apiRequest<ExerciseProgressHub>("/api/progress/exercises", {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export function getExerciseProgress(exerciseId: string, authToken: string): Promise<ExerciseProgress> {
  return apiRequest<ExerciseProgress>(`/api/progress/exercises/${encodeURIComponent(exerciseId)}`, {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export function getExerciseProgressTrend(
  exerciseId: string,
  range: { from?: string; to?: string; timeZone: string },
  authToken: string,
): Promise<ExerciseProgressTrend> {
  const searchParams = new URLSearchParams({ timeZone: range.timeZone });
  if (range.from && range.to) {
    searchParams.set("from", range.from);
    searchParams.set("to", range.to);
  }

  return apiRequest<ExerciseProgressTrend>(
    `/api/progress/exercises/${encodeURIComponent(exerciseId)}/trend?${searchParams.toString()}`,
    {
      authToken,
      cache: "no-store",
      method: "GET",
    },
  );
}
