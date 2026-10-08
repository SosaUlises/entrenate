import { apiRequest } from "@/services/api-client";
import type {
  CreateTrainingProfileRequest,
  CreateTrainingProfileResponse,
  TrainingProfileResponse,
  UpdateTrainingProfileRequest,
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

export function updateTrainingProfile(
  authToken: string,
  request: UpdateTrainingProfileRequest,
): Promise<void> {
  return apiRequest<void, UpdateTrainingProfileRequest>("/api/training-profile", {
    authToken,
    body: request,
    cache: "no-store",
    method: "PUT",
  });
}
