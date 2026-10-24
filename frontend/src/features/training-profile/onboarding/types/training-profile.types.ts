import type { UpdateTrainingProfileRequest } from "../../shared/training-profile.types";

export type CreateTrainingProfileRequest = UpdateTrainingProfileRequest;

export type CreateTrainingProfileResponse = {
  id: string;
};

export type {
  TrainingProfileEquipment,
  TrainingProfileResponse,
  UpdateTrainingProfileRequest,
} from "../../shared/training-profile.types";
