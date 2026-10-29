import { apiRequest } from "@/services/api-client";
import type {
  AskSessionAdaptationQuestionRequest,
  SessionAdaptationConversationResponse,
  SessionAdaptationProposalResponse,
  SessionAdaptationRequest,
} from "../types/adaptive-training.types";

export function generateSessionAdaptationProposal(
  request: SessionAdaptationRequest,
  authToken: string,
): Promise<SessionAdaptationProposalResponse> {
  return apiRequest<SessionAdaptationProposalResponse, SessionAdaptationRequest>(
    "/api/coach/adaptive-training/proposals",
    {
      authToken,
      body: request,
      cache: "no-store",
      method: "POST",
    },
  );
}

export function askSessionAdaptationQuestion(
  proposalId: string,
  proposalHash: string,
  request: SessionAdaptationRequest,
  question: string,
  authToken: string,
): Promise<SessionAdaptationConversationResponse> {
  return apiRequest<
    SessionAdaptationConversationResponse,
    AskSessionAdaptationQuestionRequest
  >(
    `/api/coach/adaptive-training/proposals/${encodeURIComponent(proposalId)}/conversation`,
    {
      authToken,
      body: {
        ...request,
        proposalHash,
        question,
      },
      cache: "no-store",
      method: "POST",
    },
  );
}
