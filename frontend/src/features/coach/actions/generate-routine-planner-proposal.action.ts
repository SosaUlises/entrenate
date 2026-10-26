"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { generateRoutinePlannerProposal } from "../services/routine-planner.service";
import type { RoutinePlannerProposalResponse } from "../types/routine-planner.types";

export type GenerateRoutinePlannerProposalActionResult =
  | { proposal: RoutinePlannerProposalResponse; status: "ready" }
  | { status: "error" | "unauthenticated" };

export async function generateRoutinePlannerProposalAction(): Promise<GenerateRoutinePlannerProposalActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    return {
      proposal: await generateRoutinePlannerProposal(authToken),
      status: "ready",
    };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
