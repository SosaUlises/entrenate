"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import type { PostSessionCoachSummary } from "../types/live-coach.types";
import { answerPostSessionCoachQuestion, getPostSessionCoachSummary } from "../services/live-coach.service";

export type PostSessionSummaryResult =
  | { status: "ready"; summary: PostSessionCoachSummary }
  | { status: "unauthenticated" | "not-found" | "error" };

export async function getPostSessionCoachSummaryAction(sessionId: string): Promise<PostSessionSummaryResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };
  try {
    return { status: "ready", summary: await getPostSessionCoachSummary(sessionId, token) };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }
    if (error instanceof ApiClientError && error.status === 404) return { status: "not-found" };
    return { status: "error" };
  }
}

export async function answerPostSessionCoachQuestionAction(
  sessionId: string,
  question: string,
): Promise<{ status: "ready"; answer: string } | { status: "unauthenticated" | "error" }> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };
  try {
    const response = await answerPostSessionCoachQuestion(sessionId, question, token);
    return { status: "ready", answer: response.answer };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }
    return { status: "error" };
  }
}
