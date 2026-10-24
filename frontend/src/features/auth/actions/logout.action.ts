"use server";

import { clearAuthSession } from "../services/session.service";

export async function logoutAction(): Promise<void> {
  await clearAuthSession();
}
