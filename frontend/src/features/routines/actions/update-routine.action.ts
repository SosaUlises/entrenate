"use server";

import { revalidatePath } from "next/cache";
import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { updateRoutine } from "../services/routines.service";
import type { UpdateRoutineRequest } from "../types/routine.types";
import { getValidationMessages } from "./validation-messages";

export type UpdateRoutineActionResult =
  | { status: "updated" }
  | { status: "validation-error"; messages: string[] }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function updateRoutineAction(id: string, request: UpdateRoutineRequest): Promise<UpdateRoutineActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    await updateRoutine(id, request, token);
    revalidatePath(`/routines/${id}`);
    revalidatePath("/routines");
    return { status: "updated" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) return { status: "not-found" };
      if (error.status === 400) {
        return { status: "validation-error", messages: getValidationMessages(error.details) };
      }
    }
    return { status: "error" };
  }
}
