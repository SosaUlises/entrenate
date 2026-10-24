"use server";

import { revalidatePath } from "next/cache";
import { clearAuthSession, getAuthToken } from "@/features/auth/services/session.service";
import { ApiClientError } from "@/services/api-client";
import { deleteRoutine } from "../services/routines.service";

export type DeleteRoutineActionResult =
  | { status: "deleted" }
  | { status: "conflict" }
  | { status: "not-found" }
  | { status: "unauthenticated" }
  | { status: "error" };

export async function deleteRoutineAction(id: string): Promise<DeleteRoutineActionResult> {
  const token = await getAuthToken();
  if (!token) return { status: "unauthenticated" };

  try {
    await deleteRoutine(id, token);
    revalidatePath("/home");
    revalidatePath("/routines");
    revalidatePath(`/routines/${id}`);
    return { status: "deleted" };
  } catch (error) {
    if (error instanceof ApiClientError) {
      if (error.status === 401) {
        await clearAuthSession();
        return { status: "unauthenticated" };
      }
      if (error.status === 404) return { status: "not-found" };
      if (error.status === 409) return { status: "conflict" };
    }

    return { status: "error" };
  }
}
