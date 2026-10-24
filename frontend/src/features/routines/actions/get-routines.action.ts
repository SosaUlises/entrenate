"use server";

import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { getRoutines } from "../services/routines.service";
import type { RoutineListItem } from "../types/routine.types";

export type GetRoutinesActionResult =
  | { status: "ready"; routines: RoutineListItem[] }
  | { status: "error" }
  | { status: "unauthenticated" };

export async function getRoutinesAction(): Promise<GetRoutinesActionResult> {
  const token = await getAuthToken();

  if (!token) {
    return { status: "unauthenticated" };
  }

  try {
    return { status: "ready", routines: await getRoutines(token) };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
