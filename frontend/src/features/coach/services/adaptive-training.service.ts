import { apiRequest } from "@/services/api-client";
import type {
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
