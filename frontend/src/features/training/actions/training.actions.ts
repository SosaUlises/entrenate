"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import {
  cancelTrainingSession, completeTrainingSession, getActiveTrainingSession,
  startTrainingSession, upsertTrainingSet,
} from "../services/training.service";
import type { TrainingSession, TrainingSet, TrainingSetInput } from "../types/training.types";

type Failure = { status: "unauthenticated" | "not-found" | "conflict" | "validation-error" | "error" };
type Result<T> = { status: "success"; data: T } | Failure;

async function run<T>(operation: (token: string) => Promise<T>): Promise<Result<T>> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };
  try {
    return { status: "success", data: await operation(token) };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) return { status: "not-found" };
      if (error.status === 409) return { status: "conflict" };
      if (error.status === 400) return { status: "validation-error" };
    }
    return { status: "error" };
  }
}

export async function startTrainingSessionAction(diaRutinaId: string): Promise<Result<TrainingSession>> {
  return run((token) => startTrainingSession(diaRutinaId, token));
}

export async function getActiveTrainingSessionAction(): Promise<Result<TrainingSession>> {
  return run(getActiveTrainingSession);
}

export async function upsertTrainingSetAction(sessionId: string, exerciseSessionId: string, setNumber: number, input: TrainingSetInput): Promise<Result<TrainingSet>> {
  return run((token) => upsertTrainingSet(sessionId, exerciseSessionId, setNumber, input, token));
}

export async function completeTrainingSessionAction(id: string): Promise<Result<void>> {
  return run((token) => completeTrainingSession(id, token));
}

export async function cancelTrainingSessionAction(id: string): Promise<Result<void>> {
  return run((token) => cancelTrainingSession(id, token));
}
