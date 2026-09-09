import { apiRequest } from "@/services/api-client";
import type {
  CreateTrainingProfileRequest,
  CreateTrainingProfileResponse,
  TrainingProfileResponse,
} from "../types/training-profile.types";

export function getMyTrainingProfile(
  authToken: string,
): Promise<TrainingProfileResponse> {
  return apiRequest<TrainingProfileResponse>("/api/training-profile/me", {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export function createTrainingProfile(
  authToken: string,
  request: CreateTrainingProfileRequest,
): Promise<CreateTrainingProfileResponse> {
  return apiRequest<CreateTrainingProfileResponse, CreateTrainingProfileRequest>(
    "/api/training-profile",
    {
      authToken,
      body: request,
      cache: "no-store",
      method: "POST",
    },
  );
}
