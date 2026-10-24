import { apiRequest } from "@/services/api-client";
import type {
  AskCoachProgressQuestionRequest,
  CoachProgressConversationResponse,
  CoachProgressResponse,
} from "../types/coach-progress.types";

export function getCoachProgress(
  exerciseId: string,
  authToken: string,
): Promise<CoachProgressResponse> {
  return apiRequest<CoachProgressResponse>(
    `/api/coach/progress/exercises/${encodeURIComponent(exerciseId)}`,
    {
      authToken,
      cache: "no-store",
      method: "GET",
    },
  );
}

export function askCoachProgressQuestion(
  exerciseId: string,
  question: string,
  authToken: string,
): Promise<CoachProgressConversationResponse> {
  return apiRequest<
    CoachProgressConversationResponse,
    AskCoachProgressQuestionRequest
  >(
    `/api/coach/progress/exercises/${encodeURIComponent(exerciseId)}/conversation`,
    {
      authToken,
      body: { question },
      cache: "no-store",
      method: "POST",
    },
  );
}
