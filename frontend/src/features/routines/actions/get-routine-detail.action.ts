"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getRoutineById } from "../services/routines.service";
import type { RoutineDetail } from "../types/routine.types";

export type GetRoutineDetailActionResult =
  | { status: "ready"; routine: RoutineDetail }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function getRoutineDetailAction(id: string): Promise<GetRoutineDetailActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    return { status: "ready", routine: await getRoutineById(id, token) };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) return { status: "not-found" };
    }
    return { status: "error" };
  }
}
