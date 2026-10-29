"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { askSessionAdaptationQuestion } from "../services/adaptive-training.service";
import type { SessionAdaptationRequest } from "../types/adaptive-training.types";

export type AskSessionAdaptationQuestionActionResult =
  | { answer: string; status: "ready" }
  | { status: "error" | "stale" | "unauthenticated" };

export async function askSessionAdaptationQuestionAction(
  proposalId: string,
  proposalHash: string,
  request: SessionAdaptationRequest,
  question: string,
): Promise<AskSessionAdaptationQuestionActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    const response = await askSessionAdaptationQuestion(
      proposalId,
      proposalHash,
      request,
      question,
      authToken,
    );

    return { answer: response.answer, status: "ready" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }

      if (error.status === 409) return { status: "stale" };
    }

    return { status: "error" };
  }
}
