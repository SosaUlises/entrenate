"use server";

import { revalidatePath } from "next/cache";
import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { createRoutine } from "../services/routines.service";
import type { CreateRoutineRequest } from "../types/routine.types";
import { getValidationMessages } from "./validation-messages";

export type CreateRoutineActionResult =
  | { status: "created"; id: string }
  | { status: "validation-error"; messages: string[] }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function createRoutineAction(request: CreateRoutineRequest): Promise<CreateRoutineActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    const { id } = await createRoutine(request, token);
    revalidatePath("/routines");
    return { status: "created", id };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 400) {
        return { status: "validation-error", messages: getValidationMessages(error.details) };
      }
    }
    return { status: "error" };
  }
}
