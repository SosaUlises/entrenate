import { apiRequest } from "@/services/api-client";
import type { Exercise } from "../types/exercise.types";

export function getExercises(authToken: string): Promise<Exercise[]> {
  return apiRequest<Exercise[]>("/api/exercises", {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}
