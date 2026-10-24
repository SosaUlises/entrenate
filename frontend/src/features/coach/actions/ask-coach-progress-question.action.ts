"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { askCoachProgressQuestion } from "../services/coach-progress.service";

export type AskCoachProgressQuestionActionResult =
  | { status: "ready"; answer: string }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function askCoachProgressQuestionAction(
  exerciseId: string,
  question: string,
): Promise<AskCoachProgressQuestionActionResult> {
  const token = await getAuthToken();

  if (!token) {
    return { status: "unauthenticated" };
  }

  try {
    const response = await askCoachProgressQuestion(
      exerciseId,
      question,
      token,
    );

    return { answer: response.answer, status: "ready" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }

      if (error.status === 404) {
        return { status: "not-found" };
      }
    }

    return { status: "error" };
  }
}
