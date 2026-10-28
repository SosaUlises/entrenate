"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { askRoutinePlannerQuestion } from "../services/routine-planner.service";

export type AskRoutinePlannerQuestionActionResult =
  | { answer: string; status: "ready" }
  | { status: "error" | "stale" | "unauthenticated" };

export async function askRoutinePlannerQuestionAction(
  proposalId: string,
  proposalHash: string,
  question: string,
): Promise<AskRoutinePlannerQuestionActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    const response = await askRoutinePlannerQuestion(
      proposalId,
      proposalHash,
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

      if (error.status === 409) {
        return { status: "stale" };
      }
    }

    return { status: "error" };
  }
}
