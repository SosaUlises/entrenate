import type { TrainingSession } from "@/features/training/types/training.types";
import { apiRequest } from "@/services/api-client";
import type { TrainingSessionHistorySummary } from "../types/history.types";

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
