import { apiRequest } from "@/services/api-client";
import type { TrainingSession, TrainingSet, TrainingSetInput } from "../types/training.types";

export function startTrainingSession(diaRutinaId: string, authToken: string): Promise<TrainingSession> {
  return apiRequest<TrainingSession, { diaRutinaId: string }>("/api/training-sessions", {
    authToken, body: { diaRutinaId }, cache: "no-store", method: "POST",
  });
}

export function getActiveTrainingSession(authToken: string): Promise<TrainingSession> {
  return apiRequest<TrainingSession>("/api/training-sessions/active", {
    authToken, cache: "no-store", method: "GET",
  });
}

export function upsertTrainingSet(sessionId: string, exerciseSessionId: string, setNumber: number, input: TrainingSetInput, authToken: string): Promise<TrainingSet> {
  return apiRequest<TrainingSet, TrainingSetInput>(`/api/training-sessions/${encodeURIComponent(sessionId)}/exercises/${encodeURIComponent(exerciseSessionId)}/sets/${setNumber}`, {
    authToken, body: input, cache: "no-store", method: "PUT",
  });
}

export async function completeTrainingSession(id: string, authToken: string): Promise<void> {
  await apiRequest<void>(`/api/training-sessions/${encodeURIComponent(id)}/complete`, {
    authToken, cache: "no-store", method: "POST",
  });
}

export async function cancelTrainingSession(id: string, authToken: string): Promise<void> {
  await apiRequest<void>(`/api/training-sessions/${encodeURIComponent(id)}/cancel`, {
    authToken, cache: "no-store", method: "POST",
  });
}
