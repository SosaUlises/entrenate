"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { evaluateLiveCoachProactive } from "../services/live-coach.service";
import type {
  LiveCoachProactiveEvaluation,
  LiveCoachProactiveRequest,
} from "../types/live-coach.types";

export type EvaluateLiveCoachProactiveActionResult =
  | { data: LiveCoachProactiveEvaluation; status: "success" }
  | { status: "error" | "unauthenticated" };

export async function evaluateLiveCoachProactiveAction(
  payload: LiveCoachProactiveRequest,
): Promise<EvaluateLiveCoachProactiveActionResult> {
  const authToken = await getAuthToken();
  if (!authToken) return { status: "unauthenticated" };

  try {
    return {
      data: await evaluateLiveCoachProactive(payload, authToken),
      status: "success",
    };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }
    return { status: "error" };
  }
}
