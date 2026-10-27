import { apiRequest } from "@/services/api-client";
import type {
  AskRoutinePlannerQuestionRequest,
  RoutinePlannerConversationResponse,
  RoutinePlannerProposalResponse,
} from "../types/routine-planner.types";

export function generateRoutinePlannerProposal(
  authToken: string,
): Promise<RoutinePlannerProposalResponse> {
  return apiRequest<RoutinePlannerProposalResponse>(
    "/api/coach/routine-planner/proposals",
    {
      authToken,
      cache: "no-store",
      method: "POST",
    },
  );
}

export function askRoutinePlannerQuestion(
  proposalId: string,
  proposalHash: string,
  question: string,
  authToken: string,
): Promise<RoutinePlannerConversationResponse> {
  return apiRequest<
    RoutinePlannerConversationResponse,
    AskRoutinePlannerQuestionRequest
  >(
    `/api/coach/routine-planner/proposals/${encodeURIComponent(proposalId)}/conversation`,
    {
      authToken,
      body: { proposalHash, question },
      cache: "no-store",
      method: "POST",
    },
  );
}
