"use server";

import {
  clearAuthSession,
  getAuthToken,
} from "@/features/auth/services/session.service";
import { getActiveTrainingSession } from "@/features/training/services/training.service";
import type { TrainingSession } from "@/features/training/types/training.types";
import { ApiClientError } from "@/services/api-client";
import { confirmSessionAdaptationProposal } from "../services/adaptive-training.service";
import type {
  SessionAdaptationConfirmationResponse,
  SessionAdaptationRequest,
} from "../types/adaptive-training.types";

export type ConfirmSessionAdaptationProposalActionResult =
  | {
      confirmation: SessionAdaptationConfirmationResponse;
      status: "ready";
    }
  | { session: TrainingSession; status: "active-session" }
  | { status: "error" | "stale" | "unauthenticated" };

export async function confirmSessionAdaptationProposalAction(
  proposalId: string,
  proposalHash: string,
  request: SessionAdaptationRequest,
): Promise<ConfirmSessionAdaptationProposalActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) return { status: "unauthenticated" };

  try {
    return {
      confirmation: await confirmSessionAdaptationProposal(
        proposalId,
        proposalHash,
        request,
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
        try {
          return {
            session: await getActiveTrainingSession(authToken),
            status: "active-session",
          };
        } catch (activeSessionError) {
          if (
            activeSessionError instanceof ApiClientError &&
            activeSessionError.status === 401
          ) {
            await clearAuthSession();
            return { status: "unauthenticated" };
          }

          if (
            activeSessionError instanceof ApiClientError &&
            activeSessionError.status === 404
          ) {
            return { status: "stale" };
          }

          return { status: "error" };
        }
      }
    }

    return { status: "error" };
  }
}
