"use server";

import { ApiClientError } from "@/services/api-client";
import { getCurrentUser } from "../services/auth.service";
import { clearAuthSession, getAuthToken } from "../services/session.service";
import type { CurrentUser } from "../types/auth.types";

export type GetCurrentUserActionResult =
  | { status: "ready"; user: CurrentUser }
  | { status: "error" | "unauthenticated" };

export async function getCurrentUserAction(): Promise<GetCurrentUserActionResult> {
  const token = await getAuthToken();

  if (!token) {
    return { status: "unauthenticated" };
  }

  try {
    const user = await getCurrentUser(token);
    return { status: "ready", user };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      await clearAuthSession();
      return { status: "unauthenticated" };
    }

    return { status: "error" };
  }
}
