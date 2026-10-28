"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { confirmRoutinePlannerProposal } from "../services/routine-planner.service";
import type { RoutinePlannerConfirmationResponse } from "../types/routine-planner.types";

export type ConfirmRoutinePlannerProposalActionResult =
  | {
      confirmation: RoutinePlannerConfirmationResponse;
      status: "ready";
    }
  | { status: "error" | "stale" | "unauthenticated" };

export async function confirmRoutinePlannerProposalAction(
  proposalId: string,
  proposalHash: string,
): Promise<ConfirmRoutinePlannerProposalActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    return {
      confirmation: await confirmRoutinePlannerProposal(
        proposalId,
        proposalHash,
        authToken,
      ),
      status: "ready",
    };
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
