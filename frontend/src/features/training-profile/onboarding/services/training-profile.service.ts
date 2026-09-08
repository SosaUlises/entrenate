import { apiRequest } from "@/services/api-client";
import type {
  CreateTrainingProfileRequest,
  CreateTrainingProfileResponse,
} from "../types/training-profile.types";

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
