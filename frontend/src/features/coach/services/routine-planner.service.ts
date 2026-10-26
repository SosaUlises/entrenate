import { apiRequest } from "@/services/api-client";
import type { RoutinePlannerProposalResponse } from "../types/routine-planner.types";

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
