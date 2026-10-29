"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { generateSessionAdaptationProposal } from "../services/adaptive-training.service";
import type {
  SessionAdaptationProposalResponse,
  SessionAdaptationRequest,
} from "../types/adaptive-training.types";

export type GenerateSessionAdaptationProposalActionResult =
  | { proposal: SessionAdaptationProposalResponse; status: "ready" }
  | { status: "error" | "unauthenticated" | "validation-error" };

export async function generateSessionAdaptationProposalAction(
  request: SessionAdaptationRequest,
): Promise<GenerateSessionAdaptationProposalActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    return {
      proposal: await generateSessionAdaptationProposal(request, authToken),
      status: "ready",
    };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    if (error instanceof ApiClientError && error.status === 400) {
      return { status: "validation-error" };
    }

    return { status: "error" };
  }
}
