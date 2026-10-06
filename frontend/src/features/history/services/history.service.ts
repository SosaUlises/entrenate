import type { TrainingSession } from "@/features/training/types/training.types";
import { apiRequest } from "@/services/api-client";
import type {
  TrainingActivity,
  TrainingActivityRequest,
  TrainingSessionHistorySummary,
} from "../types/history.types";

export function getTrainingHistory(authToken: string): Promise<TrainingSessionHistorySummary[]> {
  return apiRequest<TrainingSessionHistorySummary[]>("/api/training-sessions/history", {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export function getTrainingSessionById(id: string, authToken: string): Promise<TrainingSession> {
  return apiRequest<TrainingSession>(`/api/training-sessions/${encodeURIComponent(id)}`, {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export function getTrainingActivity(
  request: TrainingActivityRequest,
  authToken: string,
): Promise<TrainingActivity> {
  const searchParams = new URLSearchParams({
    from: request.from,
    to: request.to,
    timeZone: request.timeZone,
  });

  return apiRequest<TrainingActivity>(`/api/training-sessions/activity?${searchParams.toString()}`, {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}
